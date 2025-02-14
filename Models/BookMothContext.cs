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

    public virtual DbSet<Iachistory> Iachistories { get; set; }

    public virtual DbSet<PaymentInvoice> PaymentInvoices { get; set; }

    public virtual DbSet<Post> Posts { get; set; }

    public virtual DbSet<Profile> Profiles { get; set; }

    public virtual DbSet<PurchaseInvoice> PurchaseInvoices { get; set; }

    public virtual DbSet<Wallet> Wallets { get; set; }

    public virtual DbSet<Work> Works { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Data Source=_1504_\\SQLEXPRESS;Initial Catalog=BookMoth;Integrated Security=True;Trust Server Certificate=True");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(e => e.AccountId).HasName("PK__Accounts__46A222CD11C08D60");

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
        });

        modelBuilder.Entity<AuthorWallet>(entity =>
        {
            entity.HasKey(e => e.WalletId).HasName("PK__AuthorWa__0EE6F0415E9EE22C");

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
            entity.HasKey(e => e.CategoryId).HasName("PK__Categori__D54EE9B411D872A7");

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
            entity.HasKey(e => e.ChapterId).HasName("PK__Chapters__745EFE87DCD257B7");

            entity.Property(e => e.ChapterId).HasColumnName("chapter_id");
            entity.Property(e => e.FileUrl)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("file_url");
            entity.Property(e => e.PostDate)
                .HasColumnType("datetime")
                .HasColumnName("post_date");
        });

        modelBuilder.Entity<Iachistory>(entity =>
        {
            entity.HasKey(e => e.IachId).HasName("PK__IACHisto__566E7C53D51B1D9F");

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
            entity.Property(e => e.WalletId).HasColumnName("wallet_id");

            entity.HasOne(d => d.Wallet).WithMany(p => p.Iachistories)
                .HasForeignKey(d => d.WalletId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__IACHistor__walle__73BA3083");
        });

        modelBuilder.Entity<PaymentInvoice>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("PK__PaymentI__ED1FC9EAC50CCD1B");

            entity.Property(e => e.PaymentId).HasColumnName("payment_id");
            entity.Property(e => e.Amount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("amount");
            entity.Property(e => e.AuthorWalletId).HasColumnName("author_wallet_id");
            entity.Property(e => e.BankInvoiceCode)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("bank_invoice_code");
            entity.Property(e => e.BeginBalance)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("begin_balance");
            entity.Property(e => e.EndBalance)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("end_balance");
            entity.Property(e => e.PaymentDate)
                .HasColumnType("datetime")
                .HasColumnName("payment_date");

            entity.HasOne(d => d.AuthorWallet).WithMany(p => p.PaymentInvoices)
                .HasForeignKey(d => d.AuthorWalletId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PaymentIn__autho__76969D2E");
        });

        modelBuilder.Entity<Post>(entity =>
        {
            entity.HasKey(e => e.PostId).HasName("PK__Posts__3ED78766E882315D");

            entity.Property(e => e.PostId).HasColumnName("post_id");
            entity.Property(e => e.AuthorId).HasColumnName("author_id");

            entity.HasOne(d => d.Author).WithMany(p => p.Posts)
                .HasForeignKey(d => d.AuthorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Posts__author_id__656C112C");
        });

        modelBuilder.Entity<Profile>(entity =>
        {
            entity.HasKey(e => e.ProfileId).HasName("PK__Profiles__AEBB701FA26566E5");

            entity.HasIndex(e => e.Identifier, "UQ__Profiles__D112ED48154BAA16").IsUnique();

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
                .IsUnicode(false)
                .HasColumnName("first_name");
            entity.Property(e => e.Identifier)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("identifier");
            entity.Property(e => e.LastName)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("last_name");
            entity.Property(e => e.Username)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("username");

            entity.HasOne(d => d.Account).WithMany(p => p.Profiles)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Profiles__accoun__5BE2A6F2");

            entity.HasMany(d => d.Followers).WithMany(p => p.Followings)
                .UsingEntity<Dictionary<string, object>>(
                    "Follow",
                    r => r.HasOne<Profile>().WithMany()
                        .HasForeignKey("FollowerId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__Follows__followe__68487DD7"),
                    l => l.HasOne<Profile>().WithMany()
                        .HasForeignKey("FollowingId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__Follows__followi__693CA210"),
                    j =>
                    {
                        j.HasKey("FollowerId", "FollowingId").HasName("PK__Follows__CAC186A7FFAFC33F");
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
                        .HasConstraintName("FK__Follows__followi__693CA210"),
                    l => l.HasOne<Profile>().WithMany()
                        .HasForeignKey("FollowerId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__Follows__followe__68487DD7"),
                    j =>
                    {
                        j.HasKey("FollowerId", "FollowingId").HasName("PK__Follows__CAC186A7FFAFC33F");
                        j.ToTable("Follows");
                        j.IndexerProperty<int>("FollowerId").HasColumnName("follower_id");
                        j.IndexerProperty<int>("FollowingId").HasColumnName("following_id");
                    });
        });

        modelBuilder.Entity<PurchaseInvoice>(entity =>
        {
            entity.HasKey(e => e.PurchaseId).HasName("PK__Purchase__87071CB99AB01F9F");

            entity.Property(e => e.PurchaseId).HasColumnName("purchase_id");
            entity.Property(e => e.BankInvoiceCode)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("bank_invoice_code");
            entity.Property(e => e.BeginBalance)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("begin_balance");
            entity.Property(e => e.EndBalance)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("end_balance");
            entity.Property(e => e.PurchaseDate)
                .HasColumnType("datetime")
                .HasColumnName("purchase_date");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.WalletId).HasColumnName("wallet_id");

            entity.HasOne(d => d.Wallet).WithMany(p => p.PurchaseInvoices)
                .HasForeignKey(d => d.WalletId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PurchaseI__walle__70DDC3D8");
        });

        modelBuilder.Entity<Wallet>(entity =>
        {
            entity.HasKey(e => e.WalletId).HasName("PK__Wallets__0EE6F041D3696776");

            entity.Property(e => e.WalletId).HasColumnName("wallet_id");
            entity.Property(e => e.AccountId).HasColumnName("account_id");
            entity.Property(e => e.Balance)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("balance");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("status");

            entity.HasOne(d => d.Account).WithMany(p => p.Wallets)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Wallets__account__6E01572D");
        });

        modelBuilder.Entity<Work>(entity =>
        {
            entity.HasKey(e => e.WorkId).HasName("PK__Works__110F4747DA6B6625");

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
                .HasConstraintName("FK__Works__category___628FA481");

            entity.HasOne(d => d.Chapter).WithMany(p => p.Works)
                .HasForeignKey(d => d.ChapterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Works__chapter_i__619B8048");

            entity.HasOne(d => d.Profile).WithMany(p => p.Works)
                .HasForeignKey(d => d.ProfileId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Works__profile_i__60A75C0F");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
