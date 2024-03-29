﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;

namespace EFCore.Common.Extensions
{
    /// <summary>
    /// Методы расширения для работы с миграциями
    /// </summary>
    public static class DbContextMigrationsExtensions
    {
        /// <summary>
        /// Получение первой непримененной миграции контекста в бд
        /// </summary>
        /// <param name="context">Контекст бд</param>
        /// <returns>Название миграции</returns>
        private static string GetFirstNotAppliedMigration(this DbContext context)
        {
            return context.GetNotAppliedMigrations().FirstOrDefault();
        }

        /// <summary>
        /// Получение списка непримененных миграций(названий) контекста бд
        /// </summary>
        /// <param name="context">Контекст бд</param>
        /// <returns>Список названий миграций, которые не были применены в бд</returns>
        public static IEnumerable<string> GetNotAppliedMigrations(this DbContext context)
        {
            var infrastructure = context.GetInfrastructure();
            var modelMigrations = context.GetModelMigrations();
            var historyRepository = infrastructure.GetRequiredService<IHistoryRepository>();
            var databaseMigrations = historyRepository.GetAppliedMigrations();
            return modelMigrations.Where(x => databaseMigrations.All(y => y.MigrationId != x));
        }

        /// <summary>
        /// Получение списка миграций(названий) контекста бд
        /// </summary>
        /// <param name="context">Контекст бд</param>
        /// <returns>Список названий миграций</returns>
        private static IEnumerable<string> GetModelMigrations(this DbContext context)
        {
            var infrastructure = context.GetInfrastructure();
            var migrationAssembly = infrastructure.GetRequiredService<IMigrationsAssembly>();
            return migrationAssembly.Migrations.Keys;
        }

        /// <summary>
        /// Получение последней примененной миграции контекста в бд
        /// </summary>
        /// <param name="context">Контекст бд</param>
        /// <returns>Название миграции</returns>
        public static string GetLastAppliedMigration(this DbContext context)
        {
            var infrastructure = context.GetInfrastructure();
            var modelMigrations = context.GetModelMigrations();
            var historyRepository = infrastructure.GetRequiredService<IHistoryRepository>();
            var databaseMigrations = historyRepository.GetAppliedMigrations();
            var migrations = modelMigrations.ToList();
            for (var i = 0; i < migrations.Count; ++i)
            {
                if (databaseMigrations.All(y => y.MigrationId != migrations.ElementAt(i)))
                    return i == 0 ? null : migrations.ElementAt(i - 1);
            }
            return migrations.LastOrDefault();
        }

        const string SearchPattern = "CREATE TABLE \"__EFMigrationsHistory\"";
        const string SearchReplacePattern = "CREATE TABLE IF NOT EXISTS \"__EFMigrationsHistory\"";

        /// <summary>
        /// Получение скрипта миграций
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>Текст скрипта</returns>
        public static string GetMigrationScript(this DbContext context)
        {
            var infrastructure = context.GetInfrastructure();
            var modelMigrations = context.GetModelMigrations();
            var historyRepository = infrastructure.GetRequiredService<IHistoryRepository>();
            var databaseMigrations = historyRepository.GetAppliedMigrations();

            var from = context.GetFirstNotAppliedMigration();
            if (string.IsNullOrEmpty(from))
                return "";
            var migrator = infrastructure.GetRequiredService<IMigrator>();
            return migrator.GenerateScript(from == modelMigrations.First() ? null : from).Replace(SearchPattern, SearchReplacePattern);
        }

        /// <summary>
        /// Выполнение отсутствующих миграций в бд
        /// </summary>
        /// <param name="context">Контекст бд</param>
        public static void UpdateDatabase(this DbContext context)
        {
            var infrastructure = context.GetInfrastructure();
            var migrator = infrastructure.GetRequiredService<IMigrator>();
            migrator.Migrate();
        }

        /// <summary>
        /// Получение списка незафиксированных в миграциях изменений в модели
        /// </summary>
        /// <param name="context">Контекст бд</param>
        /// <returns>Перечень изменений</returns>
        public static IReadOnlyList<MigrationOperation> GetModelDifferences(this DbContext context)
        {
            var infrastructure = context.GetInfrastructure();
            var modelDiffer = infrastructure.GetRequiredService<IMigrationsModelDiffer>();
            var model = infrastructure.GetRequiredService<IModel>();
            var migrationAssembly = infrastructure.GetRequiredService<IMigrationsAssembly>();
            var snapshot = migrationAssembly.ModelSnapshot;
            return modelDiffer.GetDifferences(snapshot.Model, model);
        }

        /// <summary>
        /// Получение списка миграций, которые отсутствуют в бд
        /// </summary>
        /// <param name="context">Контекст бд</param>
        /// <returns>Перечень миграций</returns>
        public static IEnumerable<string> GetDatabaseDifferences(this DbContext context)
        {
            var infrastructure = context.GetInfrastructure();
            var migrationAssembly = infrastructure.GetRequiredService<IMigrationsAssembly>();
            var historyRepository = infrastructure.GetRequiredService<IHistoryRepository>();
            var applied = historyRepository.GetAppliedMigrations().Select(x => x.MigrationId).ToList();
            var all = migrationAssembly.Migrations;
            return all.Keys.Except(applied);
        }
    }
}
