// using System.Runtime.CompilerServices;
// using H2Sharp.EntityFrameworkCore.Design.Internal;
// using H2Sharp.EntityFrameworkCore.Infrastructure;
// using H2Sharp.EntityFrameworkCore.TestUtilities;
//
// namespace H2Sharp.EntityFrameworkCore.Scaffolding;
//
// public class CompiledModelH2Test : CompiledModelRelationalTestBase
// {
//     protected override TestHelpers TestHelpers => H2TestHelpers.Instance;
//     protected override ITestStoreFactory TestStoreFactory => H2TestStoreFactory.Instance;
//
//     // #3087
//     public override void BigModel()
//         => Assert.Throws<InvalidOperationException>(() => base.BigModel());
//
//     // #3087
//     public override void BigModel_with_JSON_columns()
//         => Assert.Throws<InvalidOperationException>(() => base.BigModel());
//
//     // #3087
//     public override void CheckConstraints()
//         => Assert.Throws<InvalidOperationException>(() => base.BigModel());
//
//     // #3087
//     public override void DbFunctions()
//         => Assert.Throws<InvalidOperationException>(() => base.BigModel());
//
//     // #3087
//     public override void Triggers()
//         => Assert.Throws<InvalidOperationException>(() => base.BigModel());
//
//     // https://github.com/dotnet/efcore/pull/32341/files#r1485603038
//     public override void Tpc()
//         => Assert.Throws<InvalidOperationException>(() => base.Tpc());
//
//     // https://github.com/dotnet/efcore/pull/32341/files#r1485603038
//     public override void ComplexTypes()
//         => Assert.Throws<InvalidOperationException>(() => base.ComplexTypes());
//
//     protected override DbContextOptionsBuilder AddOptions(DbContextOptionsBuilder builder)
//     {
//         new H2DbContextOptionsBuilder(builder).UseNetTopologySuite();
//         return builder;
//     }
//
//     protected override void AddDesignTimeServices(IServiceCollection services)
//         => new H2NetTopologySuiteDesignTimeServices().ConfigureDesignTimeServices(services);
//
//     protected override BuildSource AddReferences(BuildSource build, [CallerFilePath] string filePath = "")
//     {
//         base.AddReferences(build);
//         build.References.Add(BuildReference.ByName("H2Sharp.EntityFrameworkCore"));
//         build.References.Add(BuildReference.ByName("H2Sharp.EntityFrameworkCore.NetTopologySuite"));
//         build.References.Add(BuildReference.ByName("H2"));
//         build.References.Add(BuildReference.ByName("NetTopologySuite"));
//         return build;
//     }
// }
