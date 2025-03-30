using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace BookMoth_Api_With_C_.Models;

public partial class BookMothContext : DbContext
{
    public BookMothContext()
    {
    }

    public BookMothContext(DbContextOptions<BookMothContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Account> Accounts { get; set; }

    public virtual DbSet<AuthorWallet> AuthorWallets { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Chapter> Chapters { get; set; }
    public virtual DbSet<FcmTokens> FcmTokens { get; set; }

    public virtual DbSet<Iachistory> Iachistories { get; set; }

    public virtual DbSet<Post> Posts { get; set; }

    public virtual DbSet<Profile> Profiles { get; set; }

    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }
    public virtual DbSet<Transactions> Transactions { get; set; }

    public virtual DbSet<Wallet> Wallets { get; set; }

    public virtual DbSet<Work> Works { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(e => e.AccountId).HasName("PK__Accounts__46A222CDED00DD8E");

            entity.HasIndex(e => e.Email, "IX_Email");

            entity.HasIndex(e => e.Email, "UQ__Accounts__AB6E6164D41FB0F9").IsUnique();

            entity.Property(e => e.AccountId).HasColumnName("account_id");
            entity.Property(e => e.AccountType).HasColumnName("account_type");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("email");
            entity.Property(e => e.Password)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("password");
            entity.Property(e => e.Salt)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("salt");
        });

        modelBuilder.Entity<AuthorWallet>(entity =>
        {
            entity.HasKey(e => e.WalletId).HasName("PK__AuthorWa__0EE6F04144049E2A");

            entity.Property(e => e.WalletId).HasColumnName("wallet_id");
            entity.Property(e => e.AccumulatedBalance)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("accumulated_balance");
            entity.Property(e => e.Managers)
                .HasMaxLength(500)
                .HasColumnName("managers");
            entity.Property(e => e.PaymentInfo)
                .HasMaxLength(500)
                .HasColumnName("payment_info");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("PK__Categori__D54EE9B403D904DE");

            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.Category1)
                .HasMaxLength(100)
                .HasColumnName("category");
            entity.Property(e => e.Tag)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("tag");
        });

        modelBuilder.Entity<Chapter>(entity =>
        {
            entity.HasKey(e => e.ChapterId).HasName("PK__Chapters__745EFE876FF90C3F");

            entity.Property(e => e.ChapterId).HasColumnName("chapter_id");
            entity.Property(e => e.FileUrl)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("file_url");
            entity.Property(e => e.PostDate)
                .HasColumnType("datetime")
                .HasColumnName("post_date");
        });

        modelBuilder.Entity<FcmTokens>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__FcmToken__3214EC072C753747");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AccountId).HasColumnName("accountid");
            entity.Property(e => e.Token)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("token");
            entity.Property(e => e.DeviceId);
        });

        modelBuilder.Entity<Iachistory>(entity =>
        {
            entity.HasKey(e => e.IachId).HasName("PK__IACHisto__566E7C535913B7B0");

            entity.ToTable("IACHistory");

            entity.Property(e => e.IachId).HasColumnName("iach_id");
            entity.Property(e => e.BeginBalance)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("begin_balance");
            entity.Property(e => e.EndBalance)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("end_balance");
            entity.Property(e => e.IachDate)
                .HasColumnType("datetime")
                .HasColumnName("iach_date");
            entity.Property(e => e.InvoiceValue)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("invoice_value");
            entity.Property(e => e.ProductCode)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("product_code");
            entity.Property(e => e.TransactionType).HasColumnName("transaction_type");
        });

       

        modelBuilder.Entity<Post>(entity =>
        {
            entity.HasKey(e => e.PostId).HasName("PK__Posts__3ED7876695AD0C51");

            entity.Property(e => e.PostId).HasColumnName("post_id");
            entity.Property(e => e.AuthorId).HasColumnName("author_id");

            entity.HasOne(d => d.Author).WithMany(p => p.Posts)
                .HasForeignKey(d => d.AuthorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Posts__author_id__6C190EBB");
        });

        modelBuilder.Entity<Profile>(entity =>
        {
            entity.HasKey(e => e.ProfileId).HasName("PK__Profiles__AEBB701F149FBCA3");

            entity.HasIndex(e => e.Identifier, "UQ__Profiles__D112ED480CE1A0A6").IsUnique();

            entity.HasIndex(e => e.Identifier, "UQ__Profiles__D112ED48DD3BF9BF").IsUnique();

            entity.Property(e => e.ProfileId).HasColumnName("profile_id");
            entity.Property(e => e.AccountId).HasColumnName("account_id");
            entity.Property(e => e.Avatar)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("avatar");
            entity.Property(e => e.Coverphoto)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("coverphoto");
            entity.Property(e => e.FirstName)
                .HasMaxLength(100)
                .HasColumnName("first_name");
            entity.Property(e => e.Identifier).HasColumnName("identifier");
            entity.Property(e => e.LastName)
                .HasMaxLength(100)
                .HasColumnName("last_name");
            entity.Property(e => e.Username)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("username");

            entity.HasOne(d => d.Account).WithMany(p => p.Profiles)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Profiles__accoun__6E01572D");

            entity.HasMany(d => d.Followers).WithMany(p => p.Followings)
                .UsingEntity<Dictionary<string, object>>(
                    "Follow",
                    r => r.HasOne<Profile>().WithMany()
                        .HasForeignKey("FollowerId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__Follows__followe__6477ECF3"),
                    l => l.HasOne<Profile>().WithMany()
                        .HasForeignKey("FollowingId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__Follows__followi__66603565"),
                    j =>
                    {
                        j.HasKey("FollowerId", "FollowingId").HasName("PK__Follows__CAC186A7ED3C8859");
                        j.ToTable("Follows");
                        j.IndexerProperty<int>("FollowerId").HasColumnName("follower_id");
                        j.IndexerProperty<int>("FollowingId").HasColumnName("following_id");
                    });

            entity.HasMany(d => d.Followings).WithMany(p => p.Followers)
                .UsingEntity<Dictionary<string, object>>(
                    "Follow",
                    r => r.HasOne<Profile>().WithMany()
                        .HasForeignKey("FollowingId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__Follows__followi__66603565"),
                    l => l.HasOne<Profile>().WithMany()
                        .HasForeignKey("FollowerId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__Follows__followe__6477ECF3"),
                    j =>
                    {
                        j.HasKey("FollowerId", "FollowingId").HasName("PK__Follows__CAC186A7ED3C8859");
                        j.ToTable("Follows");
                        j.IndexerProperty<int>("FollowerId").HasColumnName("follower_id");
                        j.IndexerProperty<int>("FollowingId").HasColumnName("following_id");
                    });
        });


        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.TokenId).HasName("PK__RefreshT__CB3C9E177B545548");

            entity.HasIndex(e => e.Token, "IX_RefreshTokens_Token");

            entity.Property(e => e.TokenId).HasColumnName("token_id");
            entity.Property(e => e.AccountId).HasColumnName("account_id");
            entity.Property(e => e.CreatedByIp).HasMaxLength(45);
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ExpiryDate).HasColumnType("datetime");
            entity.Property(e => e.RevokedByIp).HasMaxLength(45);
            entity.Property(e => e.RevokedDate).HasColumnType("datetime");
            entity.Property(e => e.Token).HasMaxLength(512);

            entity.HasOne(d => d.Account).WithMany(p => p.RefreshTokens)
                .HasForeignKey(d => d.AccountId)
                .HasConstraintName("FK_RefreshTokens_Accounts");
        });

        modelBuilder.Entity<Transactions>(entity =>
        {
            entity.HasKey(e => e.TransactionId).HasName("PK__Transact__85C600AFE98B2B63");
            entity.Property(e => e.TransactionId).HasColumnName("transaction_id");
            entity.Property(e => e.Amount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("amount");
            entity.Property(e => e.Created_At);
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.TransactionType).HasColumnName("transaction_type");
            entity.Property(e => e.Description).HasColumnName("description");
        });

        modelBuilder.Entity<Wallet>(entity =>
        {
            entity.HasKey(e => e.WalletId).HasName("PK__Wallets__0EE6F041A981C8AC");

            entity.Property(e => e.WalletId).HasColumnName("wallet_id");
            entity.Property(e => e.AccountId).HasColumnName("account_id");
            entity.Property(e => e.Balance)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("balance");
            entity.Property(e => e.HashedPin);
            entity.Property(e => e.Salt);
            entity.Property(e => e.Status).HasColumnName("status");

            entity.HasOne(d => d.Account).WithMany(p => p.Wallets)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Wallets__account__72C60C4A");
        });

        modelBuilder.Entity<Work>(entity =>
        {
            entity.HasKey(e => e.WorkId).HasName("PK__Works__110F47476C258B79");

            entity.Property(e => e.WorkId).HasColumnName("work_id");
            entity.Property(e => e.AvatarUrl)
                .HasMaxLength(500)
                .HasColumnName("avatarUrl");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.ChapterId).HasColumnName("chapter_id");
            entity.Property(e => e.PostDate)
                .HasColumnType("datetime")
                .HasColumnName("post_date");
            entity.Property(e => e.Price)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("price");
            entity.Property(e => e.ProfileId).HasColumnName("profile_id");
            entity.Property(e => e.ViewCount).HasColumnName("view_count");

            entity.HasOne(d => d.Category).WithMany(p => p.Works)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Works__category___74AE54BC");

            entity.HasOne(d => d.Chapter).WithMany(p => p.Works)
                .HasForeignKey(d => d.ChapterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Works__chapter_i__76969D2E");

            entity.HasOne(d => d.Profile).WithMany(p => p.Works)
                .HasForeignKey(d => d.ProfileId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Works__profile_i__787EE5A0");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
