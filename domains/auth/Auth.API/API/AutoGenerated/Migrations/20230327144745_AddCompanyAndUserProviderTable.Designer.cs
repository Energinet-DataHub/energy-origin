﻿// <auto-generated />
using System;
using API.Repositories.Data;
using EnergyOrigin.TokenValidation.Values;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace API.AutoGenerated.Migrations
{
    [DbContext(typeof(DataContext))]
    [Migration("20230327144745_AddCompanyAndUserProviderTable")]
    partial class AddCompanyAndUserProviderTable
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "7.0.3")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.HasPostgresEnum(modelBuilder, "provider_key_type", new[] { "pid", "rid", "mit_id_uuid" });
            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("API.Models.Entities.Company", b =>
                {
                    b.Property<Guid?>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Tin")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("Tin");

                    b.ToTable("Companies");
                });

            modelBuilder.Entity("API.Models.Entities.User", b =>
                {
                    b.Property<Guid?>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<int>("AcceptedTermsVersion")
                        .HasColumnType("integer");

                    b.Property<bool>("AllowCPRLookup")
                        .HasColumnType("boolean");

                    b.Property<Guid?>("CompanyId")
                        .HasColumnType("uuid");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("CompanyId");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("API.Models.Entities.UserProvider", b =>
                {
                    b.Property<Guid?>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<ProviderKeyType>("ProviderKeyType")
                        .HasColumnType("provider_key_type");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid");

                    b.Property<string>("UserProviderKey")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.HasIndex("ProviderKeyType", "UserProviderKey", "UserId")
                        .IsUnique();

                    b.ToTable("UserProviders");
                });

            modelBuilder.Entity("API.Models.Entities.User", b =>
                {
                    b.HasOne("API.Models.Entities.Company", "Company")
                        .WithMany("Users")
                        .HasForeignKey("CompanyId");

                    b.Navigation("Company");
                });

            modelBuilder.Entity("API.Models.Entities.UserProvider", b =>
                {
                    b.HasOne("API.Models.Entities.User", "User")
                        .WithMany("UserProviders")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("API.Models.Entities.Company", b =>
                {
                    b.Navigation("Users");
                });

            modelBuilder.Entity("API.Models.Entities.User", b =>
                {
                    b.Navigation("UserProviders");
                });
#pragma warning restore 612, 618
        }
    }
}
