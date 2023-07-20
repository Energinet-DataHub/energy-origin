﻿// <auto-generated />
using System;
using API.Repositories.Data;
using EnergyOrigin.TokenValidation.Values;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace API.AutoGenerated.Migrations
{
    [DbContext(typeof(DataContext))]
    partial class DataContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "7.0.3")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.HasPostgresEnum(modelBuilder, "company_terms_type", new[] { "terms_of_service" });
            NpgsqlModelBuilderExtensions.HasPostgresEnum(modelBuilder, "provider_key_type", new[] { "pid", "rid", "eia", "mit_id_uuid" });
            NpgsqlModelBuilderExtensions.HasPostgresEnum(modelBuilder, "user_terms_type", new[] { "privacy_policy" });
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

                    b.HasIndex("Tin")
                        .IsUnique();

                    b.ToTable("Companies");
                });

            modelBuilder.Entity("API.Models.Entities.CompanyTerms", b =>
                {
                    b.Property<Guid?>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("AcceptedVersion")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<Guid>("CompanyId")
                        .HasColumnType("uuid");

                    b.Property<CompanyTermsType>("Type")
                        .HasColumnType("company_terms_type");

                    b.HasKey("Id");

                    b.HasIndex("CompanyId");

                    b.ToTable("CompanyTerms");
                });

            modelBuilder.Entity("API.Models.Entities.Role", b =>
                {
                    b.Property<Guid?>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<bool>("IsDefault")
                        .HasColumnType("boolean");

                    b.Property<string>("Key")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<bool>("OrganizationOwner")
                        .HasColumnType("boolean");

                    b.Property<bool>("RoleAdmin")
                        .HasColumnType("boolean");

                    b.HasKey("Id");

                    b.HasIndex("Key")
                        .IsUnique();

                    b.ToTable("Roles");
                });

            modelBuilder.Entity("API.Models.Entities.User", b =>
                {
                    b.Property<Guid?>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<bool>("AllowCprLookup")
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

                    b.HasIndex("ProviderKeyType", "UserProviderKey")
                        .IsUnique();

                    b.ToTable("UserProviders");
                });

            modelBuilder.Entity("API.Models.Entities.UserRole", b =>
                {
                    b.Property<Guid?>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<Guid>("RoleId")
                        .HasColumnType("uuid");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.HasIndex("RoleId");

                    b.HasIndex("UserId");

                    b.ToTable("UserRoles");
                });

            modelBuilder.Entity("API.Models.Entities.UserTerms", b =>
                {
                    b.Property<Guid?>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("AcceptedVersion")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<UserTermsType>("Type")
                        .HasColumnType("user_terms_type");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("UserTerms");
                });

            modelBuilder.Entity("API.Models.Entities.CompanyTerms", b =>
                {
                    b.HasOne("API.Models.Entities.Company", "Company")
                        .WithMany("CompanyTerms")
                        .HasForeignKey("CompanyId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Company");
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

            modelBuilder.Entity("API.Models.Entities.UserRole", b =>
                {
                    b.HasOne("API.Models.Entities.Role", "Role")
                        .WithMany("UserRoles")
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("API.Models.Entities.User", "User")
                        .WithMany("UserRoles")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Role");

                    b.Navigation("User");
                });

            modelBuilder.Entity("API.Models.Entities.UserTerms", b =>
                {
                    b.HasOne("API.Models.Entities.User", "User")
                        .WithMany("UserTerms")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("API.Models.Entities.Company", b =>
                {
                    b.Navigation("CompanyTerms");

                    b.Navigation("Users");
                });

            modelBuilder.Entity("API.Models.Entities.Role", b =>
                {
                    b.Navigation("UserRoles");
                });

            modelBuilder.Entity("API.Models.Entities.User", b =>
                {
                    b.Navigation("UserProviders");

                    b.Navigation("UserRoles");

                    b.Navigation("UserTerms");
                });
#pragma warning restore 612, 618
        }
    }
}
