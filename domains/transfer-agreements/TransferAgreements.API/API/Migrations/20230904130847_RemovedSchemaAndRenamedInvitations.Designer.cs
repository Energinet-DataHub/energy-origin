﻿// <auto-generated />
using System;
using API.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace API.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20230904130847_RemovedSchemaAndRenamedInvitations")]
    partial class RemovedSchemaAndRenamedInvitations
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "7.0.5")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("API.Models.Connection", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<Guid>("CompanyAId")
                        .HasColumnType("uuid");

                    b.Property<string>("CompanyATin")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<Guid>("CompanyBId")
                        .HasColumnType("uuid");

                    b.Property<string>("CompanyBTin")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("CompanyAId");

                    b.HasIndex("CompanyBId");

                    b.ToTable("Connections");
                });

            modelBuilder.Entity("API.Models.ConnectionInvitation", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasDefaultValueSql("current_timestamp at time zone 'UTC'");

                    b.Property<Guid>("SenderCompanyId")
                        .HasColumnType("uuid");

                    b.Property<string>("SenderCompanyTin")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("ConnectionInvitations");
                });

            modelBuilder.Entity("API.Models.TransferAgreement", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<DateTimeOffset?>("EndDate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<Guid>("ReceiverReference")
                        .HasColumnType("uuid");

                    b.Property<string>("ReceiverTin")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<Guid>("SenderId")
                        .HasColumnType("uuid");

                    b.Property<string>("SenderName")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("SenderTin")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTimeOffset>("StartDate")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("Id");

                    b.ToTable("TransferAgreements");
                });

            modelBuilder.Entity("API.Models.TransferAgreementHistoryEntry", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("ActorId")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("ActorName")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("AuditAction")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTimeOffset?>("EndDate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("ReceiverTin")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<Guid>("SenderId")
                        .HasColumnType("uuid");

                    b.Property<string>("SenderName")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("SenderTin")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTimeOffset>("StartDate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<Guid>("TransferAgreementId")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.HasIndex("TransferAgreementId");

                    b.ToTable("TransferAgreementHistoryEntries");
                });

            modelBuilder.Entity("API.Models.TransferAgreementHistoryEntry", b =>
                {
                    b.HasOne("API.Models.TransferAgreement", "TransferAgreement")
                        .WithMany()
                        .HasForeignKey("TransferAgreementId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("TransferAgreement");
                });
#pragma warning restore 612, 618
        }
    }
}
