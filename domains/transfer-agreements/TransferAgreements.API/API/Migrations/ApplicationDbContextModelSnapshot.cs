﻿// <auto-generated />
using System;
using API.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace API.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    partial class ApplicationDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "7.0.5")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("API.Data.TransferAgreement", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

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

                    b.HasKey("Id");

                    b.ToTable("TransferAgreements");
                });

            modelBuilder.Entity("API.Data.TransferAgreementAudit", b =>
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

                    b.Property<DateTime>("AuditDate")
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

                    b.ToTable("TransferAgreementAudits");
                });

            modelBuilder.Entity("API.Data.TransferAgreementAudit", b =>
                {
                    b.HasOne("API.Data.TransferAgreement", "TransferAgreement")
                        .WithMany("Audits")
                        .HasForeignKey("TransferAgreementId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("TransferAgreement");
                });

            modelBuilder.Entity("API.Data.TransferAgreement", b =>
                {
                    b.Navigation("Audits");
                });
#pragma warning restore 612, 618
        }
    }
}
