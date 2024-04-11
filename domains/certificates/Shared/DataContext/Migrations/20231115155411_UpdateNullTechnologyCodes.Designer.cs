// <auto-generated />
using System;
using DataContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DataContext.Migrations
{
    [DbContext(typeof(TransferDbContext))]
    [Migration("20231115155411_UpdateNullTechnologyCodes")]
    partial class UpdateNullTechnologyCodes
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "7.0.10")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("API.ContractService.CertificateIssuingContract", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<int>("ContractNumber")
                        .HasColumnType("integer");

                    b.Property<DateTimeOffset>("Created")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTimeOffset?>("EndDate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("GSRN")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("GridArea")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("MeteringPointOwner")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int>("MeteringPointType")
                        .HasColumnType("integer");

                    b.Property<DateTimeOffset>("StartDate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<byte[]>("WalletPublicKey")
                        .IsRequired()
                        .HasColumnType("bytea");

                    b.Property<string>("WalletUrl")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("GSRN", "ContractNumber")
                        .IsUnique();

                    b.ToTable("Contracts");
                });

            modelBuilder.Entity("API.Data.ConsumptionCertificate", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<byte[]>("BlindingValue")
                        .IsRequired()
                        .HasColumnType("bytea");

                    b.Property<long>("DateFrom")
                        .HasColumnType("bigint");

                    b.Property<long>("DateTo")
                        .HasColumnType("bigint");

                    b.Property<string>("GridArea")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Gsrn")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int>("IssuedState")
                        .HasColumnType("integer");

                    b.Property<string>("MeteringPointOwner")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<long>("Quantity")
                        .HasColumnType("bigint");

                    b.Property<string>("RejectionReason")
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("Gsrn", "DateFrom", "DateTo")
                        .IsUnique();

                    b.ToTable("ConsumptionCertificates");
                });

            modelBuilder.Entity("API.Data.ProductionCertificate", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<byte[]>("BlindingValue")
                        .IsRequired()
                        .HasColumnType("bytea");

                    b.Property<long>("DateFrom")
                        .HasColumnType("bigint");

                    b.Property<long>("DateTo")
                        .HasColumnType("bigint");

                    b.Property<string>("GridArea")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Gsrn")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int>("IssuedState")
                        .HasColumnType("integer");

                    b.Property<string>("MeteringPointOwner")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<long>("Quantity")
                        .HasColumnType("bigint");

                    b.Property<string>("RejectionReason")
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("Gsrn", "DateFrom", "DateTo")
                        .IsUnique();

                    b.ToTable("ProductionCertificates");
                });

            modelBuilder.Entity("API.DataSyncSyncer.Persistence.SynchronizationPosition", b =>
                {
                    b.Property<string>("GSRN")
                        .HasColumnType("text");

                    b.Property<long>("SyncedTo")
                        .HasColumnType("bigint");

                    b.HasKey("GSRN");

                    b.ToTable("SynchronizationPositions");
                });

            modelBuilder.Entity("API.ContractService.CertificateIssuingContract", b =>
                {
                    b.OwnsOne("CertificateValueObjects.Technology", "Technology", b1 =>
                        {
                            b1.Property<Guid>("CertificateIssuingContractId")
                                .HasColumnType("uuid");

                            b1.Property<string>("FuelCode")
                                .IsRequired()
                                .HasColumnType("text");

                            b1.Property<string>("TechCode")
                                .IsRequired()
                                .HasColumnType("text");

                            b1.HasKey("CertificateIssuingContractId");

                            b1.ToTable("Contracts");

                            b1.WithOwner()
                                .HasForeignKey("CertificateIssuingContractId");
                        });

                    b.Navigation("Technology");
                });

            modelBuilder.Entity("API.Data.ProductionCertificate", b =>
                {
                    b.OwnsOne("CertificateValueObjects.Technology", "Technology", b1 =>
                        {
                            b1.Property<Guid>("ProductionCertificateId")
                                .HasColumnType("uuid");

                            b1.Property<string>("FuelCode")
                                .IsRequired()
                                .HasColumnType("text");

                            b1.Property<string>("TechCode")
                                .IsRequired()
                                .HasColumnType("text");

                            b1.HasKey("ProductionCertificateId");

                            b1.ToTable("ProductionCertificates");

                            b1.WithOwner()
                                .HasForeignKey("ProductionCertificateId");
                        });

                    b.Navigation("Technology")
                        .IsRequired();
                });
#pragma warning restore 612, 618
        }
    }
}
