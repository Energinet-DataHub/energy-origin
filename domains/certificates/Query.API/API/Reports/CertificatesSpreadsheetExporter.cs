using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using API.ContractService.Clients;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using EnergyOrigin.WalletClient;
using EnergyOrigin.WalletClient.Models;

namespace API.Reports;

public class CertificatesSpreadsheetExporter
{
    private const string ExportFilename = "Certificates.xlsx";

    private const string RegistryColumnHeader = "Registry";
    private const string StreamIdColumnHeader = "StreamId";
    private const string GsrnColumnHeader = "GSRN";
    private const string StartColumnHeader = "Start";
    private const string EndColumnHeader = "End";
    private const string QuantityColumnHeader = "Quantity (wh)";
    private const string TypeColumnHeader = "Type";
    private const string GridAreaColumnHeader = "GridArea";

    private const string ProductionDeviceUniqueIdentification = "energyTag_ProductionDeviceUniqueIdentification";
    private const string CentralEuropeanTimeZoneIdentifier = "Central Europe Standard Time";

    private const string IsoDataTimeFormatString = "yyyy-MM-ddTHH:mm:sszzz";

    private readonly IWalletClient _walletClient;

    public CertificatesSpreadsheetExporter(IWalletClient walletClient)
    {
        _walletClient = walletClient;
    }

    public async Task<CertificatesSpreadsheet> Export(Guid ownerId, CancellationToken cancellationToken)
    {
        var certificates = await _walletClient.GetGranularCertificates(ownerId, cancellationToken, null);

        using (var memoryStream = new MemoryStream())
        {
            using (var doc = SpreadsheetDocument.Create(memoryStream, SpreadsheetDocumentType.Workbook))
            {
                var workBookPart = doc.AddWorkbookPart();
                workBookPart.Workbook = new Workbook();

                if (doc.WorkbookPart is null)
                {
                    throw new InvalidOperationException("WorkbookPart is null");
                }

                var sheets = doc.WorkbookPart.Workbook.AppendChild(new Sheets());
                var worksheetPart = doc.WorkbookPart.AddNewPart<WorksheetPart>();
                worksheetPart.Worksheet = new Worksheet(new SheetData());

                var relationshipIdPart = doc.WorkbookPart.GetIdOfPart(worksheetPart);
                var sheet = new Sheet() { Id = relationshipIdPart, SheetId = 1, Name = "Certificates" };
                sheets.Append(sheet);

                var sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();

                if (sheetData is null)
                {
                    throw new InvalidOperationException("SheetData is null");
                }

                AppendHeaderRow(sheetData, 1);

                int index = 2;
                foreach (var certificate in certificates!.Result)
                {
                    AppendCertificateRow(sheetData, (uint)index, certificate);
                    index++;
                }
            }

            return new CertificatesSpreadsheet(ExportFilename, memoryStream.ToArray());
        }
    }

    private void AppendHeaderRow(SheetData sheetData, uint rowIndex)
    {
        var sheetRow = new Row { RowIndex = new UInt32Value(rowIndex) };
        sheetRow.InsertAt(new Cell() { CellValue = new CellValue(RegistryColumnHeader), DataType = CellValues.String }, 0);
        sheetRow.InsertAt(new Cell() { CellValue = new CellValue(StreamIdColumnHeader), DataType = CellValues.String }, 1);
        sheetRow.InsertAt(new Cell() { CellValue = new CellValue(GsrnColumnHeader), DataType = CellValues.String }, 2);
        sheetRow.InsertAt(new Cell() { CellValue = new CellValue(StartColumnHeader), DataType = CellValues.String }, 3);
        sheetRow.InsertAt(new Cell() { CellValue = new CellValue(EndColumnHeader), DataType = CellValues.String }, 4);
        sheetRow.InsertAt(new Cell() { CellValue = new CellValue(QuantityColumnHeader), DataType = CellValues.String }, 5);
        sheetRow.InsertAt(new Cell() { CellValue = new CellValue(TypeColumnHeader), DataType = CellValues.String }, 6);
        sheetRow.InsertAt(new Cell() { CellValue = new CellValue(GridAreaColumnHeader), DataType = CellValues.String }, 7);
        sheetData.AppendChild(sheetRow);
    }

    private void AppendCertificateRow(SheetData sheetData, uint rowIndex, GranularCertificate certificate)
    {
        var gsrn = certificate.Attributes.GetValueOrDefault(ProductionDeviceUniqueIdentification) ??
                   certificate.Attributes.GetValueOrDefault("assetId");

        var sheetRow = new Row { RowIndex = new UInt32Value(rowIndex) };
        sheetRow.InsertAt(new Cell() { CellValue = new CellValue(certificate.FederatedStreamId.Registry), DataType = CellValues.String }, 0);
        sheetRow.InsertAt(new Cell() { CellValue = new CellValue(certificate.FederatedStreamId.StreamId.ToString()), DataType = CellValues.String },
            1);
        sheetRow.InsertAt(
            gsrn != null
                ? new Cell() { CellValue = new CellValue(gsrn), DataType = CellValues.String }
                : new Cell() { CellValue = new CellValue(""), DataType = CellValues.String }, 2);
        sheetRow.InsertAt(
            new Cell()
            {
                CellValue = new CellValue(GetIsoTimestampInCentralEuropeStandardTime(certificate.Start)),
                DataType = CellValues.String
            }, 3);
        sheetRow.InsertAt(
            new Cell()
            {
                CellValue = new CellValue(GetIsoTimestampInCentralEuropeStandardTime(certificate.End)),
                DataType = CellValues.String
            }, 4);
        sheetRow.InsertAt(new Cell() { CellValue = new CellValue((int)certificate.Quantity), DataType = CellValues.Number }, 5);
        sheetRow.InsertAt(new Cell() { CellValue = new CellValue(certificate.CertificateType.ToString()), DataType = CellValues.String }, 6);
        sheetRow.InsertAt(new Cell() { CellValue = new CellValue(certificate.GridArea), DataType = CellValues.String }, 7);
        sheetData.AppendChild(sheetRow);
    }

    private string GetIsoTimestampInCentralEuropeStandardTime(long timestamp)
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(CentralEuropeanTimeZoneIdentifier);
        return TimeZoneInfo.ConvertTime(DateTimeOffset.FromUnixTimeSeconds(timestamp), timeZone).ToString(IsoDataTimeFormatString, CultureInfo.InvariantCulture);
    }
}

public class CertificatesSpreadsheet
{
    public string Filename { get; private set; }

    public byte[] Bytes { get; private set; }

    public CertificatesSpreadsheet(string filename, byte[] bytes)
    {
        Filename = filename;
        Bytes = bytes;
    }
}
