﻿// <auto-generated />
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
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20240305123915_AddOtherOrganizationToActivityLogEntries")]
    partial class AddOtherOrganizationToActivityLogEntries
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.2")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("DataContext.Models.ClaimAutomationArgument", b =>
                {
                    b.Property<Guid>("SubjectId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("SubjectId");

                    b.ToTable("ClaimAutomationArguments");
                });

            modelBuilder.Entity("DataContext.Models.TransferAgreement", b =>
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

                    b.Property<int>("TransferAgreementNumber")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.HasIndex("SenderId", "TransferAgreementNumber")
                        .IsUnique();

                    b.ToTable("TransferAgreements");
                });

            modelBuilder.Entity("DataContext.Models.TransferAgreementHistoryEntry", b =>
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

            modelBuilder.Entity("DataContext.Models.TransferAgreementProposal", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasDefaultValueSql("current_timestamp at time zone 'UTC'");

                    b.Property<DateTimeOffset?>("EndDate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("ReceiverCompanyTin")
                        .HasColumnType("text");

                    b.Property<Guid>("SenderCompanyId")
                        .HasColumnType("uuid");

                    b.Property<string>("SenderCompanyName")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("SenderCompanyTin")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTimeOffset>("StartDate")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("Id");

                    b.ToTable("TransferAgreementProposals");
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

            modelBuilder.Entity("DataContext.Models.TransferAgreementHistoryEntry", b =>
                {
                    b.HasOne("DataContext.Models.TransferAgreement", "TransferAgreement")
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
