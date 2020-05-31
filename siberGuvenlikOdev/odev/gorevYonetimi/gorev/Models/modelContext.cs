using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace gorev.Models
{
    class modelContext : DbContext
    {
        public DbSet<ipLogin> ipLogin { get; set; }
        public DbSet<kullanici> kullanici { get; set; }
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ipLogin>().ToTable("ipLogin");
            modelBuilder.Entity<kullanici>().ToTable("kullanici");
        }
    }

    public class ipLogin
    {
        [Key]
        public int ipLoginId { get; set; }
        public DateTime zaman { get; set; }
        public string ipAdresi { get; set; }
        public string dogrulama { get; set; }
        public virtual kullanici Kullanici { get; set; }
    }

    public class kullanici
    {
        [Key]
        public int kullaniciId { get; set; }
        public string adSoyad { get; set; }
        public string sifre { get; set; }
        public string kullaniciAdi { get; set; }
        public string email { get; set; }
        public string soru { get; set; }
        public string cevap { get; set; }
        public string dogrulamaKodu { get; set; }
        public bool hesapDurumu { get; set; }
    }

}