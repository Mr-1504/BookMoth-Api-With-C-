using System;
using System.Collections.Generic;
using BookMoth_Api_With_C_.ResponseModels;
using Microsoft.Data.SqlClient;
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

    public virtual DbSet<FcmToken> FcmTokens { get; set; }

    public virtual DbSet<Follow> Follows { get; set; }

    public virtual DbSet<Iachistory> Iachistories { get; set; }

    public virtual DbSet<OwnershipRecord> OwnershipRecords { get; set; }

    public virtual DbSet<PaymentMethod> PaymentMethods { get; set; }

    public virtual DbSet<Post> Posts { get; set; }

    public virtual DbSet<Profile> Profiles { get; set; }

    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }

    public virtual DbSet<Transaction> Transactions { get; set; }

    public virtual DbSet<Wallet> Wallets { get; set; }

    public virtual DbSet<Work> Works { get; set; }

    public virtual DbSet<Worktag> Worktags { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(e => e.AccountId).HasName("PK__Accounts__46A222CDC3F6452A");

            entity.HasIndex(e => e.Email, "IX_Email");

            entity.HasIndex(e => e.Email, "UQ__Accounts__AB6E6164E6571808").IsUnique();

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
            entity.HasKey(e => e.WalletId).HasName("PK__AuthorWa__0EE6F041AF572669");

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
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.Tag)
                .HasMaxLength(100)
                .HasColumnName("tag");
        });

        modelBuilder.Entity<Chapter>(entity =>
        {
            entity.Property(e => e.ChapterId).HasColumnName("chapter_id");
            entity.Property(e => e.ContentUrl)
                .HasMaxLength(100)
                .HasColumnName("content_url");
            entity.Property(e => e.PostDate)
                .HasColumnType("datetime")
                .HasColumnName("post_date");
            entity.Property(e => e.Title)
                .HasMaxLength(200)
                .HasColumnName("title");
            entity.Property(e => e.WorkId).HasColumnName("work_id");

            entity.HasOne(d => d.Work).WithMany(p => p.Chapters)
                .HasForeignKey(d => d.WorkId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Chapters_Works");
        });

        modelBuilder.Entity<FcmToken>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__FcmToken__3214EC0741C7A8CF");

            entity.Property(e => e.DeviceId).HasMaxLength(100);
            entity.Property(e => e.Token).HasMaxLength(255);

            entity.HasOne(d => d.Account).WithMany(p => p.FcmTokens)
                .HasForeignKey(d => d.AccountId)
                .HasConstraintName("FK_FcmTokens_accounts");
        });

        modelBuilder.Entity<Follow>(entity =>
        {
            entity.HasKey(e => new { e.FollowerId, e.FollowingId }).HasName("PK__Follows__CAC186A7CF813938");

            entity.Property(e => e.FollowerId).HasColumnName("follower_id");
            entity.Property(e => e.FollowingId).HasColumnName("following_id");

            entity.HasOne(d => d.Following).WithMany(p => p.Follows)
                .HasForeignKey(d => d.FollowingId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Follows__followi__6A30C649");
        });

        modelBuilder.Entity<Iachistory>(entity =>
        {
            entity.HasKey(e => e.IachId).HasName("PK__IACHisto__566E7C53517DD32C");

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
            entity.Property(e => e.PaymentMethodId).HasColumnName("Payment_Method_Id");
            entity.Property(e => e.ProductCode)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("product_code");
            entity.Property(e => e.TransactionType).HasColumnName("transaction_type");

            entity.HasOne(d => d.PaymentMethod).WithMany(p => p.Iachistories)
                .HasForeignKey(d => d.PaymentMethodId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_IACHistory_PaymentMethods");

            entity.HasOne(d => d.ReceiverWallet).WithMany(p => p.IachistoryReceiverWallets)
                .HasForeignKey(d => d.ReceiverWalletId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Iachistory_Receiver");

            entity.HasOne(d => d.SenderWallet).WithMany(p => p.IachistorySenderWallets)
                .HasForeignKey(d => d.SenderWalletId)
                .HasConstraintName("FK_Iachistory_Sender");
        });

        modelBuilder.Entity<OwnershipRecord>(entity =>
        {
            entity
                .HasKey(e => new { e.AccountId, e.WorkId });

            entity.ToTable("OwnershipRecord");

            entity.Property(e => e.AccountId).HasColumnName("account_id");
            entity.Property(e => e.WorkId).HasColumnName("work_id");
            entity.Property(e => e.ExpiryDate)
                .HasColumnType("datetime")
                .HasColumnName("expiry_date");

            entity.HasOne(d => d.Account)
                .WithMany()
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OwnershipRecord_Accounts");

            entity.HasOne(d => d.Work)
                .WithMany()
                .HasForeignKey(d => d.WorkId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OwnershipRecord_Works");
        });


        modelBuilder.Entity<PaymentMethod>(entity =>
        {
            entity.HasKey(e => e.MethodId).HasName("PK__PaymentM__747727B6BE5FFAFD");

            entity.Property(e => e.MethodId).HasColumnName("method_id");
            entity.Property(e => e.MethodName)
                .HasMaxLength(50)
                .HasColumnName("method_name");
        });

        modelBuilder.Entity<Post>(entity =>
        {
            entity.HasKey(e => e.PostId).HasName("PK__Posts__3ED787668FE0DCE1");

            entity.Property(e => e.PostId).HasColumnName("post_id");
            entity.Property(e => e.AuthorId).HasColumnName("author_id");

            entity.HasOne(d => d.Author).WithMany(p => p.Posts)
                .HasForeignKey(d => d.AuthorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Posts__author_id__6FE99F9F");
        });

        modelBuilder.Entity<Profile>(entity =>
        {
            entity.HasKey(e => e.ProfileId).HasName("PK__Profiles__AEBB701FC039D968");

            entity.Property(e => e.ProfileId).HasColumnName("profile_id");
            entity.Property(e => e.AccountId).HasColumnName("account_id");
            entity.Property(e => e.Avatar)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("avatar");
            entity.Property(e => e.Birth)
                .HasColumnType("datetime")
                .HasColumnName("birth");
            entity.Property(e => e.Coverphoto)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("coverphoto");
            entity.Property(e => e.FirstName)
                .HasMaxLength(100)
                .HasColumnName("first_name");
            entity.Property(e => e.Gender).HasColumnName("gender");
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
                .HasConstraintName("FK__Profiles__accoun__71D1E811");
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.TokenId).HasName("PK__RefreshT__CB3C9E1718F9D9C6");

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

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(e => e.TransactionId).HasName("PK__Transact__85C600AF6F6D844F");

            entity.Property(e => e.TransactionId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("transaction_id");
            entity.Property(e => e.Amount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("amount");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.PaymentMethodId).HasColumnName("payment_method_id");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.TransactionType).HasColumnName("transaction_type");

            entity.HasOne(d => d.PaymentMethod).WithMany(p => p.Transactions)
                .HasForeignKey(d => d.PaymentMethodId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Transacti__payme__74AE54BC");
            entity.Property(e => e.Description).HasColumnName("description");
        });

        modelBuilder.Entity<Wallet>(entity =>
        {
            entity.HasKey(e => e.WalletId).HasName("PK__Wallets__0EE6F04171A5A22E");

            entity.Property(e => e.WalletId).HasColumnName("wallet_id");
            entity.Property(e => e.AccountId).HasColumnName("account_id");
            entity.Property(e => e.Balance)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("balance");
            entity.Property(e => e.Hashedpin)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("hashedpin");
            entity.Property(e => e.Salt)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("salt");
            entity.Property(e => e.Status).HasColumnName("status");

            entity.HasOne(d => d.Account).WithMany(p => p.Wallets)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Wallets__account__75A278F5");
        });

        modelBuilder.Entity<Work>(entity =>
        {
            entity.Property(e => e.WorkId).HasColumnName("work_id");
            entity.Property(e => e.CoverUrl)
                .HasMaxLength(100)
                .HasColumnName("cover_url");
            entity.Property(e => e.Description)
                .HasMaxLength(2000)
                .HasColumnName("description");
            entity.Property(e => e.PostDate)
                .HasColumnType("datetime")
                .HasColumnName("post_date");
            entity.Property(e => e.Price)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("price");
            entity.Property(e => e.ProfileId).HasColumnName("profile_id");
            entity.Property(e => e.Title)
                .HasMaxLength(500)
                .HasColumnName("title");
            entity.Property(e => e.ViewCount).HasColumnName("view_count");

            entity.HasOne(d => d.Profile).WithMany(p => p.Works)
                .HasForeignKey(d => d.ProfileId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Works_Profiles");
        });

        modelBuilder.Entity<Worktag>(entity =>
        {
            entity.HasNoKey();

            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.WorkId).HasColumnName("work_id");

            entity.HasOne(d => d.Category).WithMany()
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Worktags_Categories");

            entity.HasOne(d => d.Work).WithMany()
                .HasForeignKey(d => d.WorkId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Worktags_Works");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    public async Task<List<ProfileDTO>> SearchUsersByFollowAsync(int profileId, string searchString)
    {
        return await this.Database
            .SqlQueryRaw<ProfileDTO>("EXEC SearchUsersByFollow @profileId, @SearchString",
                new SqlParameter("@profileId", profileId),
                new SqlParameter("@SearchString", searchString ?? ""))  // Đảm bảo SearchString không null
            .ToListAsync();
    }


    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
