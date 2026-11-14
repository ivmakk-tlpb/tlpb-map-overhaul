using System;
using System.IO;
using System.Linq;
using MapOverhaul;
using Xunit;

public class PackedManifestTests
{
    // One entry of a manifest: the chunk, the record type, and the first byte of its 32-byte record id.
    internal static byte[] Manifest(int version = 2, int flags = 1, string magic = "BPM2", int? count = null, int extra = 0,
        params (int X, int Y, int Type, int Id)[] entries)
    {
        var stream = new MemoryStream();
        var writer = new BinaryWriter(stream);
        writer.Write(magic.Select(c => (byte)c).ToArray());
        writer.Write(version);
        writer.Write(flags);
        writer.Write(count ?? entries.Length);
        foreach (var (x, y, type, id) in entries)
        {
            writer.Write(x);
            writer.Write(y);
            writer.Write((byte)type);
            writer.Write(Enumerable.Range(0, 32).Select(i => (byte)(id + i)).ToArray());
        }
        writer.Write(0x12345678);
        writer.Write(new byte[extra]);
        return stream.ToArray();
    }

    [Fact]
    public void The_entities_record_of_a_chunk_gives_the_first_16_bytes_of_its_record_id()
    {
        var bytes = Manifest(entries: new[] { (-22, -41, 1, 10), (-22, -41, 3, 50), (5, 6, 3, 90) });

        Assert.True(PackedManifest.TryParse(bytes, out var manifest, out var problem), problem);

        Assert.True(manifest.TryGetEntities(-22, -41, out var low, out var high));
        Assert.Equal(BitConverter.ToInt64(Enumerable.Range(50, 8).Select(i => (byte)i).ToArray(), 0), low);
        Assert.Equal(BitConverter.ToInt64(Enumerable.Range(58, 8).Select(i => (byte)i).ToArray(), 0), high);
        Assert.True(manifest.TryGetEntities(5, 6, out _, out _));
        Assert.Equal(2, manifest.EntityRecords);
    }

    [Fact]
    public void A_chunk_with_no_entities_record_gives_nothing()
    {
        var bytes = Manifest(entries: new[] { (1, 2, 1, 10), (1, 2, 2, 20) });

        Assert.True(PackedManifest.TryParse(bytes, out var manifest, out _));

        Assert.False(manifest.TryGetEntities(1, 2, out _, out _));
        Assert.False(manifest.TryGetEntities(7, 7, out _, out _));
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(0, false)]
    public void The_complete_flag_comes_from_the_header(int flags, bool complete)
    {
        Assert.True(PackedManifest.TryParse(Manifest(flags: flags), out var manifest, out _));
        Assert.Equal(complete, manifest.Complete);
    }

    [Fact]
    public void Another_magic_does_not_parse_and_names_the_magic()
    {
        Assert.False(PackedManifest.TryParse(Manifest(magic: "BPK2"), out var manifest, out var problem));
        Assert.Null(manifest);
        Assert.Contains("magic", problem);
    }

    [Fact]
    public void Another_version_does_not_parse_and_names_the_version()
    {
        Assert.False(PackedManifest.TryParse(Manifest(version: 3), out _, out var problem));
        Assert.Contains("version 3", problem);
    }

    [Theory]
    [InlineData(5, 0)]
    [InlineData(null, 7)]
    public void A_length_that_does_not_fit_the_entry_count_does_not_parse(int? count, int extra)
    {
        var bytes = Manifest(count: count, extra: extra, entries: new[] { (1, 2, 3, 10) });

        Assert.False(PackedManifest.TryParse(bytes, out _, out var problem));
        Assert.Contains("length", problem);
    }

    [Fact]
    public void A_file_shorter_than_the_header_does_not_parse()
    {
        Assert.False(PackedManifest.TryParse(new byte[] { 0x42, 0x50 }, out _, out var problem));
        Assert.Contains("length", problem);
        Assert.False(PackedManifest.TryParse(null, out _, out _));
    }
}
