// using Microsoft.EntityFrameworkCore;
// using Microsoft.Extensions.Options;
//
// namespace BackbonePackagesService.Api.Configurations;
//
// /// <summary>
// /// Provides extensions for database migration.
// /// </summary>
// public static class MigrationExtensions
// {
//     #region Schema Migration
//
//     /// <summary>
//     /// Migrates the database associated with the specified context.
//     /// </summary>
//     /// <param name="serviceProvider">Service scope factory</param>
//     /// <typeparam name="TContext">Data access context</typeparam>
//     public static async ValueTask MigrateAsync<TContext>(this IServiceProvider serviceProvider) where TContext : DbContext
//     {
//         var dbContext = serviceProvider.GetRequiredService<TContext>();
//
//         if ((await dbContext.Database.GetPendingMigrationsAsync()).Any())
//             await dbContext.Database.MigrateAsync();
//
//         if (dbContext is AppDbContext appDbContext)
//         {
//             await appDbContext.SeedMedicationDefinitions(serviceProvider);
//         }
//     }
//
//     #endregion
//
//     #region Data Migration
//
//     /// <summary>
//     /// Seeds loan offers identity.
//     /// </summary>
//     /// <exception cref="ArgumentNullException">If any of default scopes, consumer or token is null.</exception>
//     internal static async ValueTask SeedMedicationDefinitions(
//         this AppDbContext dbContext,
//         IServiceProvider serviceProvider,
//         CancellationToken ct = default)
//     {
//         // Get medication settings
//         var medicationDefinitionsSettings = serviceProvider.GetRequiredService<IOptions<DefaultMedicationDefinitionsSettings>>().Value;
//
//         // Seed default medication types
//         if (!await dbContext.MedicationTypes.AnyAsync(cancellationToken: ct))
//         {
//             var medicationTypes = medicationDefinitionsSettings.MedicationTypes
//                 .Select(m => (m.TemporaryId, Entity: m.ToEntity()))
//                 .ToDictionary(t => t.TemporaryId, t => t.Entity);
//             
//             dbContext.MedicationTypes.AddRange(medicationTypes.Values);
//             
//             var vaccinationMedicationTypes = medicationDefinitionsSettings.MedicalProcedureMedications
//                 .Select(v => v.ToEntity(medicationTypes[v.MedicationTypeTemporaryId]));
//             
//             dbContext.MedicalProcedureMedications.AddRange(vaccinationMedicationTypes);
//             await dbContext.SaveChangesAsync(ct);
//         }
//         
//         var test = dbContext.MedicalProcedureMedications.ToList();
//
//         #endregion
//     }
// }