using System.Collections.Generic;
using DataContext.Models;

namespace TransferAgreementAutomation.Worker;

public class TransferAgreementProcessOrderComparer : IComparer<TransferAgreement>
{
    public int Compare(TransferAgreement? x, TransferAgreement? y)
    {
        if (x is null || y is null)
        {
            return 0;
        }
        return -1 * x.Type.CompareTo(y.Type);
    }
}
