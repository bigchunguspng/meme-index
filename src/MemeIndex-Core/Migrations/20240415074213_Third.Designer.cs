﻿// <auto-generated />
using System;
using MemeIndex_Core.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace MemeIndexCore.Migrations
{
    [DbContext(typeof(MemeDbContext))]
    [Migration("20240415074213_Third")]
    partial class Third
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "7.0.0");

            modelBuilder.Entity("MemeIndex_Core.Entities.Directory", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsTracked")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Path")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Directories");
                });

            modelBuilder.Entity("MemeIndex_Core.Entities.File", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("Created")
                        .HasColumnType("TEXT");

                    b.Property<int>("DirectoryId")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("Modified")
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<long>("Size")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("Tracked")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("DirectoryId");

                    b.ToTable("Files");
                });

            modelBuilder.Entity("MemeIndex_Core.Entities.Mean", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Code")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Means");
                });

            modelBuilder.Entity("MemeIndex_Core.Entities.Text", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("FileId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("MeanId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("WordId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("FileId");

                    b.HasIndex("MeanId");

                    b.HasIndex("WordId");

                    b.ToTable("Texts");
                });

            modelBuilder.Entity("MemeIndex_Core.Entities.Word", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Text")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("Text")
                        .IsUnique();

                    b.ToTable("Word");
                });

            modelBuilder.Entity("MemeIndex_Core.Entities.File", b =>
                {
                    b.HasOne("MemeIndex_Core.Entities.Directory", "Directory")
                        .WithMany()
                        .HasForeignKey("DirectoryId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Directory");
                });

            modelBuilder.Entity("MemeIndex_Core.Entities.Text", b =>
                {
                    b.HasOne("MemeIndex_Core.Entities.File", "File")
                        .WithMany("Texts")
                        .HasForeignKey("FileId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("MemeIndex_Core.Entities.Mean", "Mean")
                        .WithMany()
                        .HasForeignKey("MeanId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("MemeIndex_Core.Entities.Word", "Word")
                        .WithMany()
                        .HasForeignKey("WordId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("File");

                    b.Navigation("Mean");

                    b.Navigation("Word");
                });

            modelBuilder.Entity("MemeIndex_Core.Entities.File", b =>
                {
                    b.Navigation("Texts");
                });
#pragma warning restore 612, 618
        }
    }
}
