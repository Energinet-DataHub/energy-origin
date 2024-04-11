﻿// <auto-generated />
using System;
using DataContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DataContext.Migrations
{
    [DbContext(typeof(TransferDbContext))]
    partial class TransferDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.2")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("DataContext.Models.CertificateIssuingContract", b =>
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

            modelBuilder.Entity("DataContext.Models.ConsumptionCertificate", b =>
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

            modelBuilder.Entity("DataContext.Models.ProductionCertificate", b =>
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

            modelBuilder.Entity("DataContext.Models.SynchronizationPosition", b =>
                {
                    b.Property<string>("GSRN")
                        .HasColumnType("text");

                    b.Property<long>("SyncedTo")
                        .HasColumnType("bigint");

                    b.HasKey("GSRN");

                    b.ToTable("SynchronizationPositions");
                });

            modelBuilder.Entity("EnergyOrigin.ActivityLog.DataContext.ActivityLogEntry", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<int>("ActionType")
                        .HasColumnType("integer");

                    b.Property<Guid>("ActorId")
                        .HasColumnType("uuid");

                    b.Property<string>("ActorName")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int>("ActorType")
                        .HasColumnType("integer");

                    b.Property<string>("EntityId")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int>("EntityType")
                        .HasColumnType("integer");

                    b.Property<string>("OrganizationName")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("OrganizationTin")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("OtherOrganizationName")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("OtherOrganizationTin")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTimeOffset>("Timestamp")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("Id");

                    b.HasIndex("OrganizationTin")
                        .HasAnnotation("SqlServer:Clustered", false);

                    b.ToTable("ActivityLogs");
                });

            modelBuilder.Entity("MassTransit.EntityFrameworkCoreIntegration.InboxState", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("Id"));

                    b.Property<DateTime?>("Consumed")
                        .HasColumnType("timestamp with time zone");

                    b.Property<Guid>("ConsumerId")
                        .HasColumnType("uuid");

                    b.Property<DateTime?>("Delivered")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTime?>("ExpirationTime")
                        .HasColumnType("timestamp with time zone");

                    b.Property<long?>("LastSequenceNumber")
                        .HasColumnType("bigint");

                    b.Property<Guid>("LockId")
                        .HasColumnType("uuid");

                    b.Property<Guid>("MessageId")
                        .HasColumnType("uuid");

                    b.Property<int>("ReceiveCount")
                        .HasColumnType("integer");

                    b.Property<DateTime>("Received")
                        .HasColumnType("timestamp with time zone");

                    b.Property<byte[]>("RowVersion")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("bytea");

                    b.HasKey("Id");

                    b.HasAlternateKey("MessageId", "ConsumerId");

                    b.HasIndex("Delivered");

                    b.ToTable("InboxState");
                });

            modelBuilder.Entity("MassTransit.EntityFrameworkCoreIntegration.OutboxMessage", b =>
                {
                    b.Property<long>("SequenceNumber")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("SequenceNumber"));

                    b.Property<string>("Body")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("ContentType")
                        .IsRequired()
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.Property<Guid?>("ConversationId")
                        .HasColumnType("uuid");

                    b.Property<Guid?>("CorrelationId")
                        .HasColumnType("uuid");

                    b.Property<string>("DestinationAddress")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.Property<DateTime?>("EnqueueTime")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTime?>("ExpirationTime")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("FaultAddress")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.Property<string>("Headers")
                        .HasColumnType("text");

                    b.Property<Guid?>("InboxConsumerId")
                        .HasColumnType("uuid");

                    b.Property<Guid?>("InboxMessageId")
                        .HasColumnType("uuid");

                    b.Property<Guid?>("InitiatorId")
                        .HasColumnType("uuid");

                    b.Property<Guid>("MessageId")
                        .HasColumnType("uuid");

                    b.Property<string>("MessageType")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<Guid?>("OutboxId")
                        .HasColumnType("uuid");

                    b.Property<string>("Properties")
                        .HasColumnType("text");

                    b.Property<Guid?>("RequestId")
                        .HasColumnType("uuid");

                    b.Property<string>("ResponseAddress")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.Property<DateTime>("SentTime")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("SourceAddress")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.HasKey("SequenceNumber");

                    b.HasIndex("EnqueueTime");

                    b.HasIndex("ExpirationTime");

                    b.HasIndex("OutboxId", "SequenceNumber")
                        .IsUnique();

                    b.HasIndex("InboxMessageId", "InboxConsumerId", "SequenceNumber")
                        .IsUnique();

                    b.ToTable("OutboxMessage");
                });

            modelBuilder.Entity("MassTransit.EntityFrameworkCoreIntegration.OutboxState", b =>
                {
                    b.Property<Guid>("OutboxId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<DateTime>("Created")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTime?>("Delivered")
                        .HasColumnType("timestamp with time zone");

                    b.Property<long?>("LastSequenceNumber")
                        .HasColumnType("bigint");

                    b.Property<Guid>("LockId")
                        .HasColumnType("uuid");

                    b.Property<byte[]>("RowVersion")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("bytea");

                    b.HasKey("OutboxId");

                    b.HasIndex("Created");

                    b.ToTable("OutboxState");
                });

            modelBuilder.Entity("DataContext.Models.CertificateIssuingContract", b =>
                {
                    b.OwnsOne("DataContext.ValueObjects.Technology", "Technology", b1 =>
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

            modelBuilder.Entity("DataContext.Models.ProductionCertificate", b =>
                {
                    b.OwnsOne("DataContext.ValueObjects.Technology", "Technology", b1 =>
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
