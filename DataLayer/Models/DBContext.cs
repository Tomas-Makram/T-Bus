using Microsoft.EntityFrameworkCore;

namespace DataLayer.Models
{
    public class DBContext : DbContext
    {
        public DBContext() { }
        public DBContext(DbContextOptions<DBContext> options) : base(options)
        {
        }

        public DbSet<UserSession> UserSessions { get; set; }
        public DbSet<Users> Users { get; set; } = null!;
        public DbSet<Buses> Buses { get; set; } = null!;
        public DbSet<Drivers> Drivers { get; set; } = null!;
        public DbSet<PhoneNumbers> PhoneNumbers { get; set; } = null!;
        public DbSet<Trips> Trips { get; set; } = null!;

        public DbSet<Payments> Payments { get; set; } = null!;

        public DbSet<PaymentAlso> PaymentAlso { get; set; } = null!;
        public DbSet<TripTahseel> TripTahseel { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Trips>()
                .HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.AdminId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Payments>()
                .HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Payments>()
                .HasOne(p => p.Trip)
                .WithMany(t => t.Payments)
                .HasForeignKey(p => p.TripId)
                .OnDelete(DeleteBehavior.Cascade);


            modelBuilder.Entity<TripTahseel>()
                .HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TripTahseel>()
                .HasOne(t => t.Trip)
                .WithMany(t => t.TahseelItems)
                .HasForeignKey(t => t.TripId)
                .OnDelete(DeleteBehavior.Cascade);


            modelBuilder.Entity<PaymentAlso>()
                .HasOne(p => p.user)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            //modelBuilder.Entity<Trips>()
            //    .Property(x => x.Visa)
            //    .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Trips>()
                .Property(x => x.Cache)
                .HasColumnType("decimal(18,2)");

            //modelBuilder.Entity<Trips>()
            //    .Property(x => x.Octine)
            //    .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Trips>()
                .Property(x => x.TripPrice)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Trips>()
                .Property(x => x.DriverPrice)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Payments>()
                .Property(x => x.Amount)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<TripTahseel>()
                .Property(x => x.Amount)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<PaymentAlso>()
                .Property(x => x.PaymentAlsoPrice)
                .HasColumnType("decimal(18,2)");
        }

        //protected override void OnModelCreating(ModelBuilder modelBuilder)
        //{
        //    base.OnModelCreating(modelBuilder);

        //    modelBuilder.Entity<Trips>()
        //        .Property(x => x.Visa)
        //        .HasConversion<double?>();

        //    modelBuilder.Entity<Trips>()
        //        .Property(x => x.Cache)
        //        .HasConversion<double?>();

        //    modelBuilder.Entity<Trips>()
        //        .Property(x => x.Octine)
        //        .HasConversion<double?>();

        //    modelBuilder.Entity<Trips>()
        //        .Property(x => x.TripPrice)
        //        .HasConversion<double>();

        //    modelBuilder.Entity<Trips>()
        //        .Property(x => x.DriverPrice)
        //        .HasConversion<double>();

        //    modelBuilder.Entity<Payments>()
        //        .Property(x => x.Amount)
        //        .HasConversion<double>();

        //    modelBuilder.Entity<PaymentAlso>()
        //       .Property(x => x.PaymentAlsoPrice)
        //       .HasConversion<double>();
        //}
    }
}
