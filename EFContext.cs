using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace ReverseProxy.Data
{
    public class Post
    {
        [Key]
        [DatabaseGenerated(System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedOption.None)]
        public int PostID { get; set; }
        [Required]
        public string URL { get; set; }
        public bool IsSaved { get; set; }

        public bool Enabled { get; set; }
    }

    public class Request
    {
        [Key]
        public DateTime RequestTime { get; set; }
    }
    public class EFContext:System.Data.Entity.DbContext
    {
        public EFContext()
            : base("EFContext")
        {
        }
        public System.Data.Entity.DbSet<Post> Posts { get; set; }
        public System.Data.Entity.DbSet<Request> Requests { get; set; }
    }
}