﻿// <auto-generated />
using System;
using API.Models;
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
                .HasAnnotation("ProductVersion", "8.0.4")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("API.Models.Affiliation", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<Guid>("OrganizationId")
                        .HasColumnType("uuid");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.HasIndex("OrganizationId");

                    b.HasIndex("UserId");

                    b.ToTable("Affiliations");
                });

            modelBuilder.Entity("API.Models.Client", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("IdpClientId")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int>("Role")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.HasIndex("IdpClientId")
                        .IsUnique();

                    b.ToTable("Clients");
                });

            modelBuilder.Entity("API.Models.Consent", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<Guid>("ClientId")
                        .HasColumnType("uuid");

                    b.Property<DateTime>("ConsentDate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<Guid>("OrganizationId")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.HasIndex("ClientId");

                    b.HasIndex("OrganizationId");

                    b.ToTable("Consents");
                });

            modelBuilder.Entity("API.Models.Organization", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<Guid>("IdpId")
                        .HasColumnType("uuid");

                    b.Property<Guid>("IdpOrganizationId")
                        .HasColumnType("uuid");

                    b.Property<string>("OrganizationName")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("OrganizationName");

                    b.Property<string>("Tin")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("Tin");

                    b.HasKey("Id");

                    b.HasIndex("IdpOrganizationId")
                        .IsUnique();

                    b.ToTable("Organizations");
                });

            modelBuilder.Entity("API.Models.User", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("IdpId")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("IdpUserId")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("IdpUserId")
                        .IsUnique();

                    b.ToTable("Users");
                });

            modelBuilder.Entity("API.Models.Affiliation", b =>
                {
                    b.HasOne("API.Models.Organization", "Organization")
                        .WithMany("Affiliations")
                        .HasForeignKey("OrganizationId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("API.Models.User", "User")
                        .WithMany("Affiliations")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Organization");

                    b.Navigation("User");
                });

            modelBuilder.Entity("API.Models.Consent", b =>
                {
                    b.HasOne("API.Models.Client", "Client")
                        .WithMany()
                        .HasForeignKey("ClientId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("API.Models.Organization", "Organization")
                        .WithMany("Consents")
                        .HasForeignKey("OrganizationId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Client");

                    b.Navigation("Organization");
                });

            modelBuilder.Entity("API.Models.Organization", b =>
                {
                    b.Navigation("Affiliations");

                    b.Navigation("Consents");
                });

            modelBuilder.Entity("API.Models.User", b =>
                {
                    b.Navigation("Affiliations");
                });
#pragma warning restore 612, 618
        }
    }
}
