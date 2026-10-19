using Microsoft.EntityFrameworkCore.ValueGeneration.Internal;
using H2Sharp.EntityFrameworkCore.TestUtilities;

namespace H2Sharp.EntityFrameworkCore;

public class H2ValueGenerationScenariosTest
{
    private static readonly string DatabaseName = "H2ValueGenerationScenariosTest";

    [Fact]
    public async Task Insert_with_sequence_id()
    {
        await using var testStore = await H2TestStore.CreateInitializedAsync(DatabaseName);

        using (var context = new BlogContextSequence(testStore.Name))
        {
            context.Database.EnsureCreated();
            context.AddRange(
                new Blog { Name = "One Unicorn" },
                new Blog { Name = "Two Unicorns" });
            context.SaveChanges();
        }

        using (var context = new BlogContextSequence(testStore.Name))
        {
            var blogs = context.Blogs.OrderBy(e => e.Id).ToList();

            Assert.Equal(1, blogs[0].Id);
            Assert.Equal(2, blogs[1].Id);
        }
    }


    [Fact]
    public async Task Insert_with_sequence_HiLo()
    {
        await using var testStore = await H2TestStore.CreateInitializedAsync(DatabaseName);

        using (var context = new BlogContextHiLo(testStore.Name))
        {
            context.Database.EnsureCreated();
            context.AddRange(
                new Blog { Name = "One Unicorn" },
                new Blog { Name = "Two Unicorns" });
            context.SaveChanges();
        }

        using (var context = new BlogContextHiLo(testStore.Name))
        {
            var blogs = context.Blogs.OrderBy(e => e.Id).ToList();

            Assert.Equal(1, blogs[0].Id);
            Assert.Equal(2, blogs[1].Id);
        }
    }


    [Fact]
    public async Task Insert_with_default_value_from_sequence()
    {
        await using var testStore = await H2TestStore.CreateInitializedAsync(DatabaseName);

        using (var context = new BlogContextDefaultValue(testStore.Name))
        {
            context.Database.EnsureCreated();
            context.AddRange(
                new Blog { Name = "One Unicorn" },
                new Blog { Name = "Two Unicorns" });
            context.SaveChanges();
        }

        using (var context = new BlogContextDefaultValue(testStore.Name))
        {
            var blogs = context.Blogs.OrderBy(e => e.Id).ToList();

            Assert.Equal(0, blogs[0].Id);
            Assert.Equal(1, blogs[1].Id);
        }

        using (var context = new BlogContextDefaultValueNoMigrations(testStore.Name))
        {
            context.AddRange(
                new Blog { Name = "One Unicorn" },
                new Blog { Name = "Two Unicorns" });
            context.SaveChanges();
        }

        using (var context = new BlogContextDefaultValueNoMigrations(testStore.Name))
        {
            var blogs = context.Blogs.OrderBy(e => e.Id).ToList();

            Assert.Equal(0, blogs[0].Id);
            Assert.Equal(1, blogs[1].Id);
            Assert.Equal(2, blogs[2].Id);
            Assert.Equal(3, blogs[3].Id);
        }
    }



    public class BlogWithStringKey
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }

    [Fact]
    public async Task Insert_with_key_default_value_from_sequence()
    {
        await using var testStore = await H2TestStore.CreateInitializedAsync(DatabaseName);

        using (var context = new BlogContextKeyColumnWithDefaultValue(testStore.Name))
        {
            context.Database.EnsureCreated();
            context.AddRange(
                new Blog { Name = "One Unicorn" },
                new Blog { Name = "Two Unicorns" });
            context.SaveChanges();
        }

        using (var context = new BlogContextKeyColumnWithDefaultValue(testStore.Name))
        {
            var blogs = context.Blogs.OrderBy(e => e.Id).ToList();

            Assert.Equal(77, blogs[0].Id);
            Assert.Equal(78, blogs[1].Id);
        }
    }


    [ConditionalFact]
    public async Task Insert_uint_to_Identity_column_using_value_converter()
    {
        await using var testStore = await H2TestStore.CreateInitializedAsync(DatabaseName);
        using (var context = new BlogContextUIntToIdentityUsingValueConverter(testStore.Name))
        {
            context.Database.EnsureCreatedResiliently();

            context.AddRange(
                new BlogWithUIntKey { Name = "One Unicorn" }, new BlogWithUIntKey { Name = "Two Unicorns" });

            context.SaveChanges();
        }

        using (var context = new BlogContextUIntToIdentityUsingValueConverter(testStore.Name))
        {
            var blogs = context.UnsignedBlogs.OrderBy(e => e.Id).ToList();

            Assert.Equal((uint)1, blogs[0].Id);
            Assert.Equal((uint)2, blogs[1].Id);
        }
    }


    public class BlogWithUIntKey
    {
        public uint Id { get; set; }
        public string Name { get; set; }
    }

    [ConditionalFact]
    public async Task Insert_string_to_Identity_column_using_value_converter()
    {
        await using var testStore = await H2TestStore.CreateInitializedAsync(DatabaseName);
        using (var context = new BlogContextStringToIdentityUsingValueConverter(testStore.Name))
        {
            context.Database.EnsureCreatedResiliently();

            context.AddRange(
                new BlogWithStringKey { Name = "One Unicorn" }, new BlogWithStringKey { Name = "Two Unicorns" });

            context.SaveChanges();
        }

        using (var context = new BlogContextStringToIdentityUsingValueConverter(testStore.Name))
        {
            var blogs = context.StringyBlogs.OrderBy(e => e.Id).ToList();

            Assert.Equal("1", blogs[0].Id);
            Assert.Equal("2", blogs[1].Id);
        }
    }


    [Fact]
    public async Task  Insert_with_explicit_non_default_keys()
    {
        await using var testStore = await H2TestStore.CreateInitializedAsync(DatabaseName);

        using (var context = new BlogContextNoKeyGeneration(testStore.Name))
        {
            context.Database.EnsureCreated();
            context.AddRange(
                new Blog { Id = 66, Name = "One Unicorn" },
                new Blog { Id = 67, Name = "Two Unicorns" });
            context.SaveChanges();
        }

        using (var context = new BlogContextNoKeyGeneration(testStore.Name))
        {
            var blogs = context.Blogs.OrderBy(e => e.Id).ToList();

            Assert.Equal(66, blogs[0].Id);
            Assert.Equal(67, blogs[1].Id);
        }
    }


    [Fact]
    public async Task  Insert_with_explicit_with_default_keys()
    {
        await using var testStore = await H2TestStore.CreateInitializedAsync(DatabaseName);

        using (var context = new BlogContextNoKeyGenerationNullableKey(testStore.Name))
        {
            context.Database.EnsureCreated();
            context.AddRange(
                new NullableKeyBlog { Id = 0, Name = "One Unicorn" },
                new NullableKeyBlog { Id = 1, Name = "Two Unicorns" });
            context.SaveChanges();
        }

        using (var context = new BlogContextNoKeyGenerationNullableKey(testStore.Name))
        {
            var blogs = context.NullableKeyBlogs.OrderBy(e => e.Id).ToList();

            Assert.Equal(0, blogs[0].Id);
            Assert.Equal(1, blogs[1].Id);
        }
    }


    [Fact]
    public async Task  Insert_with_non_key_default_value()
    {
        await using var testStore = await H2TestStore.CreateInitializedAsync(DatabaseName);

        using (var context = new BlogContextNonKeyDefaultValue(testStore.Name))
        {
            context.Database.EnsureCreated();
            var blogs = new List<Blog>
            {
                new() { Name = "One Unicorn" }, new() { Name = "Two Unicorns", CreatedOn = new DateTime(1969, 8, 3, 0, 10, 0) }
            };
            context.AddRange(blogs);
            context.SaveChanges();

            Assert.NotEqual(new DateTime(), blogs[0].CreatedOn);
            Assert.NotEqual(new DateTime(), blogs[1].CreatedOn);
        }

        using (var context = new BlogContextNonKeyDefaultValue(testStore.Name))
        {
            var blogs = context.Blogs.OrderBy(e => e.Name).ToList();

            Assert.NotEqual(new DateTime(), blogs[0].CreatedOn);
            Assert.Equal(new DateTime(1969, 8, 3, 0, 10, 0), blogs[1].CreatedOn);

            blogs[0].CreatedOn = new DateTime(1973, 9, 3, 0, 10, 0);
            blogs[1].Name = "Zwo Unicorns";
            context.SaveChanges();
        }

        using (var context = new BlogContextNonKeyDefaultValue(testStore.Name))
        {
            var blogs = context.Blogs.OrderBy(e => e.Name).ToList();

            Assert.Equal(new DateTime(1969, 8, 3, 0, 10, 0), blogs[1].CreatedOn);
            Assert.Equal(new DateTime(1973, 9, 3, 0, 10, 0), blogs[0].CreatedOn);
        }
    }


    [Fact]
    public async Task  Insert_with_non_key_default_value_readonly()
    {
        await using var testStore = await H2TestStore.CreateInitializedAsync(DatabaseName);

        using (var context = new BlogContextNonKeyReadOnlyDefaultValue(testStore.Name))
        {
            context.Database.EnsureCreated();
            context.AddRange(
                new Blog { Name = "One Unicorn" },
                new Blog { Name = "Two Unicorns" });
            context.SaveChanges();
            Assert.NotEqual(new DateTime(), context.Blogs.ToList()[0].CreatedOn);
        }

        DateTime dateTime0;

        using (var context = new BlogContextNonKeyReadOnlyDefaultValue(testStore.Name))
        {
            var blogs = context.Blogs.OrderBy(e => e.Id).ToList();

            dateTime0 = blogs[0].CreatedOn;

            Assert.NotEqual(new DateTime(), dateTime0);
            Assert.NotEqual(new DateTime(), blogs[1].CreatedOn);

            blogs[0].Name = "One Pegasus";
            blogs[1].CreatedOn = new DateTime(1973, 9, 3, 0, 10, 0);

            context.SaveChanges();
        }

        using (var context = new BlogContextNonKeyReadOnlyDefaultValue(testStore.Name))
        {
            var blogs = context.Blogs.OrderBy(e => e.Id).ToList();

            Assert.Equal(dateTime0, blogs[0].CreatedOn);
            Assert.Equal(new DateTime(1973, 9, 3, 0, 10, 0), blogs[1].CreatedOn);
        }
    }


    [Fact]
    public async Task  Insert_with_serial_non_id()
    {
        await using var testStore = await H2TestStore.CreateInitializedAsync(DatabaseName);

        int afterSave;

        using (var context = new BlogContextSequenceNonId(testStore.Name))
        {
            context.Database.EnsureCreated();

            var blog = context.Add(new Blog { Name = "One Unicorn" }).Entity;
            var beforeSave = blog.OtherId;
            context.SaveChanges();
            afterSave = blog.OtherId;

            Assert.NotEqual(beforeSave, afterSave);
        }

        using (var context = new BlogContextSequenceNonId(testStore.Name))
        {
            Assert.Equal(afterSave, context.Blogs.Single().OtherId);
        }
    }


    [Fact]
    public async Task  Insert_with_client_generated_GUID_key()
    {
        await using var testStore = await H2TestStore.CreateInitializedAsync(DatabaseName);

        Guid afterSave;
        using (var context = new BlogContext(testStore.Name))
        {
            context.Database.EnsureCreated();
            var blog = context.Add(new GuidBlog { Name = "One Unicorn" }).Entity;
            var beforeSave = blog.Id;
            context.SaveChanges();
            afterSave = blog.Id;

            Assert.Equal(beforeSave, afterSave);
        }

        using (var context = new BlogContext(testStore.Name))
        {
            Assert.Equal(afterSave, context.GuidBlogs.Single().Id);
        }
    }


    [Fact]
    public async Task  Insert_with_server_generated_GUID_key()
    {
        await using var testStore = await H2TestStore.CreateInitializedAsync(DatabaseName);

        Guid afterSave;
        using (var context = new BlogContextServerGuidKey(testStore.Name))
        {
            context.Database.EnsureCreated();

            var blog = context.Add(
                new GuidBlog { Name = "One Unicorn" }).Entity;
            var beforeSave = blog.Id;
            var beforeSaveNotId = blog.NotId;

            Assert.Equal(default, beforeSave);
            Assert.Equal(default, beforeSaveNotId);

            context.SaveChanges();

            afterSave = blog.Id;
            var afterSaveNotId = blog.NotId;

            Assert.NotEqual(default, afterSave);
            Assert.NotEqual(default, afterSaveNotId);
            Assert.NotEqual(beforeSave, afterSave);
            Assert.NotEqual(beforeSaveNotId, afterSaveNotId);
        }

        using (var context = new BlogContextServerGuidKey(testStore.Name))
        {
            Assert.Equal(afterSave, context.GuidBlogs.Single().Id);
        }
    }


    public class Blog
    {
        public int Id { get; set; }
        public int OtherId { get; set; }
        public string Name { get; set; }
        public DateTime CreatedOn { get; set; }
    }

    public class NullableKeyBlog
    {
        public int? Id { get; set; }
        public string Name { get; set; }
        public DateTime CreatedOn { get; set; }
    }

    public class FullNameBlog
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName { get; set; }
    }

    public class GuidBlog
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public Guid NotId { get; set; }
    }

    public class ConcurrentBlog
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public byte[] Timestamp { get; set; }
    }

}
