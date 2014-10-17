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
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }
        public int? DanbooruPostID { get; set; }
        [Required]
        public string URL { get; set; }
        public bool IsSaved { get; set; }
    }

    public class Request
    {
        public DateTime RequestTime { get; set; }
    }
    public class EFContext:System.Data.Entity.DbContext
    {
        public System.Data.Entity.DbSet<Post> Posts { get; set; }
        public System.Data.Entity.DbSet<Request> Requests { get; set; }
    }
}