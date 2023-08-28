﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using newstrek.Data;

#nullable disable

namespace newstrek.Migrations
{
    [DbContext(typeof(NewsTrekDbContext))]
    partial class NewsTrekDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "7.0.10")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("newstrek.Models.InterestedTopic", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"));

                    b.Property<long>("UserId")
                        .HasColumnType("bigint");

                    b.Property<bool?>("business")
                        .HasColumnType("bit");

                    b.Property<bool?>("climate")
                        .HasColumnType("bit");

                    b.Property<bool?>("entertainment")
                        .HasColumnType("bit");

                    b.Property<bool?>("health")
                        .HasColumnType("bit");

                    b.Property<bool?>("history")
                        .HasColumnType("bit");

                    b.Property<bool?>("politics")
                        .HasColumnType("bit");

                    b.Property<bool?>("science")
                        .HasColumnType("bit");

                    b.Property<bool?>("sports")
                        .HasColumnType("bit");

                    b.Property<bool?>("tech")
                        .HasColumnType("bit");

                    b.Property<bool?>("world")
                        .HasColumnType("bit");

                    b.HasKey("Id");

                    b.HasIndex("UserId")
                        .IsUnique();

                    b.ToTable("InterestedTopics");
                });

            modelBuilder.Entity("newstrek.Models.News", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"));

                    b.Property<string>("Article")
                        .IsRequired()
                        .HasColumnType("varchar(max)");

                    b.Property<string>("Category")
                        .HasColumnType("varchar(max)");

                    b.Property<string>("Date")
                        .HasColumnType("varchar(max)");

                    b.Property<string>("Tag")
                        .HasColumnType("varchar(max)");

                    b.Property<string>("Title")
                        .HasColumnType("varchar(max)");

                    b.Property<string>("URL")
                        .HasColumnType("varchar(max)");

                    b.HasKey("Id");

                    b.ToTable("News");
                });

            modelBuilder.Entity("newstrek.Models.User", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"));

                    b.Property<long>("AccessExpired")
                        .HasColumnType("bigint");

                    b.Property<string>("AccessToken")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime?>("LoginAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Password")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Provider")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("newstrek.Models.InterestedTopic", b =>
                {
                    b.HasOne("newstrek.Models.User", "User")
                        .WithOne("InterestedTopic")
                        .HasForeignKey("newstrek.Models.InterestedTopic", "UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("newstrek.Models.User", b =>
                {
                    b.Navigation("InterestedTopic")
                        .IsRequired();
                });
#pragma warning restore 612, 618
        }
    }
}
