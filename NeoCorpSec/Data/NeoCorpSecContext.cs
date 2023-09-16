using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NeoCorpSec.Models.Authenitcation;

namespace NeoCorpSec.Data
{
    public class NeoCorpSecContext : DbContext
    {
        public NeoCorpSecContext (DbContextOptions<NeoCorpSecContext> options)
            : base(options)
        {
        }

        public DbSet<NeoCorpSec.Models.Authenitcation.SecurityUser> SecurityUser { get; set; } = default!;
    }
}
