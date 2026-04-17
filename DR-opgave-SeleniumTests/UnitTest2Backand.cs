using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecordsRepo;
using Xunit;
using Record = DR.Data.Record;
using RecordDbContext = DR.Data.RecordDbContext;

namespace TestBackend;

// ─────────────────────────────────────────────────────────────────────────────
// RecordRepository (in-memory list) — pure unit tests, no database required
// ─────────────────────────────────────────────────────────────────────────────
public class RecordRepositoryTests
{
    private static Record MakeRecord(string name = "Abbey Road", string artist = "The Beatles",
        string genre = "Rock", int year = 1969, int tracks = 17, int duration = 2822) =>
        new Record { Name = name, Artist = artist, Genre = genre, ReleaseYear = year, trackCount = tracks, Duration = duration };

    // GetAll returns empty list on a fresh repository
    [Fact]
    public void GetAll_EmptyRepository_ReturnsEmptyList()
    {
        var repo = new RecordRepository();

        var result = repo.GetAll();

        Assert.Empty(result);
    }

    // Add assigns an Id starting at 1
    [Fact]
    public void Add_FirstRecord_AssignsIdOne()
    {
        var repo = new RecordRepository();
        var record = MakeRecord();

        var added = repo.Add(record);

        Assert.Equal(1, added.Id);
    }

    // Add increments Id for subsequent records
    [Fact]
    public void Add_SecondRecord_AssignsIdTwo()
    {
        var repo = new RecordRepository();
        repo.Add(MakeRecord("Record A"));

        var second = repo.Add(MakeRecord("Record B"));

        Assert.Equal(2, second.Id);
    }

    // GetAll returns all added records
    [Fact]
    public void GetAll_AfterAddingTwoRecords_ReturnsBoth()
    {
        var repo = new RecordRepository();
        repo.Add(MakeRecord("A"));
        repo.Add(MakeRecord("B"));

        var result = repo.GetAll();

        Assert.Equal(2, result.Count());
    }

    // GetById returns the correct record
    [Fact]
    public void GetById_ExistingId_ReturnsRecord()
    {
        var repo = new RecordRepository();
        var added = repo.Add(MakeRecord("Thriller", "Michael Jackson"));

        var found = repo.GetById(added.Id);

        Assert.NotNull(found);
        Assert.Equal("Thriller", found.Name);
    }

    // GetById returns null for a missing id
    [Fact]
    public void GetById_NonExistingId_ReturnsNull()
    {
        var repo = new RecordRepository();

        var result = repo.GetById(999);

        Assert.Null(result);
    }

    // Delete removes and returns the record
    [Fact]
    public void Delete_ExistingId_RemovesAndReturnsRecord()
    {
        var repo = new RecordRepository();
        var added = repo.Add(MakeRecord());

        var deleted = repo.Delete(added.Id);

        Assert.NotNull(deleted);
        Assert.Equal(added.Id, deleted.Id);
        Assert.Empty(repo.GetAll());
    }

    // Delete returns null when id does not exist
    [Fact]
    public void Delete_NonExistingId_ReturnsNull()
    {
        var repo = new RecordRepository();

        var result = repo.Delete(999);

        Assert.Null(result);
    }

    // Update changes all fields and returns the updated record
    [Fact]
    public void Update_ExistingId_UpdatesAllFields()
    {
        var repo = new RecordRepository();
        var added = repo.Add(MakeRecord());

        var updated = new Record
        {
            Name = "Let It Be",
            Artist = "The Beatles",
            Genre = "Rock",
            ReleaseYear = 1970,
            trackCount = 12,
            Duration = 2400
        };

        var result = repo.Update(added.Id, updated);

        Assert.NotNull(result);
        Assert.Equal("Let It Be", result.Name);
        Assert.Equal(1970, result.ReleaseYear);
        Assert.Equal(12, result.trackCount);
    }

    // Update returns null when the record does not exist
    [Fact]
    public void Update_NonExistingId_ReturnsNull()
    {
        var repo = new RecordRepository();

        var result = repo.Update(999, MakeRecord());

        Assert.Null(result);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// RecordsControllerv2 — tests using EF Core InMemory database
// Note: search tests are skipped because ILike is PostgreSQL-specific and
//       is not supported by the InMemory provider.
// ─────────────────────────────────────────────────────────────────────────────
public class RecordsControllerv2Tests : IDisposable
{
    private readonly RecordDbContext _db;
    private readonly RecordRepoDB _repo;
    private readonly RecordsControllerv2 _controller;

    public RecordsControllerv2Tests()
    {
        var options = new DbContextOptionsBuilder<RecordDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _db = new RecordDbContext(options);
        _repo = new RecordRepoDB(_db);
        _controller = new RecordsControllerv2(_repo);
    }

    public void Dispose() => _db.Dispose();

    private Record SeedRecord(string name = "Dark Side of the Moon", string artist = "Pink Floyd",
        string genre = "Prog Rock", int year = 1973, int tracks = 10, int duration = 2580)
    {
        var record = new Record { Name = name, Artist = artist, Genre = genre, ReleaseYear = year, trackCount = tracks, Duration = duration };
        _db.Records.Add(record);
        _db.SaveChanges();
        return record;
    }

    // GET /api/v2/records — returns 200 OK with all records
    [Fact]
    public void GetAll_NoSearch_Returns200WithAllRecords()
    {
        SeedRecord("Record A");
        SeedRecord("Record B");

        var result = _controller.GetAll(null);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var records = Assert.IsAssignableFrom<IEnumerable<Record>>(ok.Value);
        Assert.Equal(2, records.Count());
    }

    // GET /api/v2/records — empty database returns 200 with empty list
    [Fact]
    public void GetAll_EmptyDatabase_Returns200WithEmptyList()
    {
        var result = _controller.GetAll(null);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var records = Assert.IsAssignableFrom<IEnumerable<Record>>(ok.Value);
        Assert.Empty(records);
    }

    // POST /api/v2/records — valid body returns 201 Created with the new record
    [Fact]
    public void Create_ValidRecord_Returns201WithRecord()
    {
        var newRecord = new Record { Name = "Rumours", Artist = "Fleetwood Mac", Genre = "Rock", ReleaseYear = 1977, trackCount = 11, Duration = 2400 };

        var result = _controller.Create(newRecord);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(201, created.StatusCode);
        var body = Assert.IsType<Record>(created.Value);
        Assert.Equal("Rumours", body.Name);
        Assert.True(body.Id > 0);
    }

    // POST /api/v2/records — null body returns 400 Bad Request
    [Fact]
    public void Create_NullRecord_Returns400()
    {
        var result = _controller.Create(null!);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    // DELETE /api/v2/records/{id} — existing id returns 200 with the deleted record
    [Fact]
    public void Delete_ExistingId_Returns200WithDeletedRecord()
    {
        var record = SeedRecord();

        var result = _controller.Delete(record.Id);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<Record>(ok.Value);
        Assert.Equal(record.Id, body.Id);
        Assert.Empty(_db.Records.ToList());
    }

    // DELETE /api/v2/records/{id} — missing id returns 404 Not Found
    [Fact]
    public void Delete_NonExistingId_Returns404()
    {
        var result = _controller.Delete(999);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    // PUT /api/v2/records/{id} — valid update returns 200 with updated fields
    [Fact]
    public void Update_ExistingId_Returns200WithUpdatedRecord()
    {
        var record = SeedRecord();
        var updated = new Record { Name = "Animals", Artist = "Pink Floyd", Genre = "Rock", ReleaseYear = 1977, trackCount = 5, Duration = 2580 };

        var result = _controller.Update(record.Id, updated);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<Record>(ok.Value);
        Assert.Equal("Animals", body.Name);
        Assert.Equal(1977, body.ReleaseYear);
    }

    // PUT /api/v2/records/{id} — missing id returns 404 Not Found
    [Fact]
    public void Update_NonExistingId_Returns404()
    {
        var updated = new Record { Name = "Ghost", Artist = "Unknown", Genre = "Pop", ReleaseYear = 2020, trackCount = 8, Duration = 1800 };

        var result = _controller.Update(999, updated);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    // PUT /api/v2/records/{id} — null body returns 400 Bad Request
    [Fact]
    public void Update_NullRecord_Returns400()
    {
        var result = _controller.Update(1, null!);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }
}
