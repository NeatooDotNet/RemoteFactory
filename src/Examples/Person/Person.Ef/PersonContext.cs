
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.ComponentModel.DataAnnotations;

namespace Person.Ef;

public interface IPersonContext
{
	DbSet<PersonEntity> Persons { get; set; }

	Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public class PersonContext : DbContext, IPersonContext
{
	public virtual DbSet<PersonEntity> Persons { get; set; } = null!;

	public string DbPath { get; }

	public PersonContext()
	{
		var folder = Environment.SpecialFolder.LocalApplicationData;
		var path = Environment.GetFolderPath(folder);
		this.DbPath = System.IO.Path.Join(path, "Person.db");
	}

	// The following configures EF to create a Sqlite database file in the
	// special "local" folder for your platform.
	protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		 => optionsBuilder.UseSqlite($"Data Source={this.DbPath}")
		 .UseLazyLoadingProxies();
}

public class PersonEntity
{
	[Key]
	public int Id { get; set; } = 1;
	[Required]
	public string FirstName { get; set; } = null!;
	[Required]
	public string LastName { get; set; } = null!;
	public string? Email { get; set; }
	public string? Phone { get; set; }
	public string? Notes { get; set; }
	public DateTime Created { get; set; }
	public DateTime Modified { get; set; }
}
