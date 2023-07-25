﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Notifier.Database.Database;

#nullable disable

namespace Notifier.BackgroundService.Host.Migrations
{
    [DbContext(typeof(NContext))]
    [Migration("20230719122710_Users_ChatId_Changes_To_Long")]
    partial class Users_ChatId_Changes_To_Long
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "7.0.5")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            modelBuilder.Entity("Notifier.Database.Database.Entities.MovieRecord", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Info")
                        .HasColumnType("longtext");

                    b.Property<bool>("IsRemoved")
                        .HasColumnType("tinyint(1)");

                    b.Property<int?>("LastEpisode")
                        .HasColumnType("int");

                    b.Property<int?>("LastSeason")
                        .HasColumnType("int");

                    b.Property<string>("Link")
                        .HasColumnType("longtext");

                    b.Property<bool>("Notified")
                        .HasColumnType("tinyint(1)");

                    b.Property<DateTime?>("NotifiedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("State")
                        .HasColumnType("int");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("datetime(6)");

                    b.HasKey("Id");

                    b.ToTable("MoviesRecords");
                });

            modelBuilder.Entity("Notifier.Database.Database.Entities.User", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<long>("ChatId")
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.ToTable("Users");
                });
#pragma warning restore 612, 618
        }
    }
}
