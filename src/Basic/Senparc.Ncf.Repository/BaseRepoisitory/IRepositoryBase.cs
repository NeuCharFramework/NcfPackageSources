/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: IRepositoryBase.cs
    File description: IRepositoryBase related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261001
    Change description: Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.20.10-preview9 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.20.13-preview12 Updated repository infrastructure and related localization metadata

    Modified by: Senparc - 20261127
    Change description: v0.20.13.2-preview13 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Storage;
using Senparc.Ncf.Core.DI;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Senparc.Ncf.Repository
{
    /// <summary>
    /// Defines synchronous and asynchronous persistence, query, aggregation, paging,
    /// deletion, and transaction operations for entities of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The entity type managed by the repository.</typeparam>
    public interface IRepositoryBase<T> : IDataBase, IAutoDI where T : class, IEntityBase
    {
        /// <summary>Determines whether an entity should be inserted rather than updated.</summary>
        /// <param name="obj">The entity to inspect.</param>
        /// <returns><see langword="true"/> when the entity is new to the current unit of work.</returns>
        bool IsInsert(T obj);

        /// <summary>Builds an ordered query over all entities.</summary>
        /// <typeparam name="TOrderProperty">The ordering property type.</typeparam>
        /// <param name="orderBy">The ordering expression.</param>
        /// <param name="orderingType">The ordering direction.</param>
        /// <param name="includes">String navigation-property paths to include.</param>
        /// <returns>An executable entity query.</returns>
        IQueryable<T> GeAll<TOrderProperty>(Expression<Func<T, TOrderProperty>> orderBy, OrderingType orderingType, params string[] includes);

        /// <summary>Builds an ordered query using a strongly typed include expression.</summary>
        /// <typeparam name="TOrderProperty">The ordering property type.</typeparam>
        /// <typeparam name="TIncludesProperty">The included navigation property type.</typeparam>
        /// <param name="orderBy">The ordering expression.</param>
        /// <param name="orderingType">The ordering direction.</param>
        /// <param name="includesNavigationPropertyPathFunc">The typed include expression.</param>
        /// <returns>An executable entity query.</returns>
        IQueryable<T> GeAll<TOrderProperty, TIncludesProperty>(Expression<Func<T, TOrderProperty>> orderBy, OrderingType orderingType, Expression<Func<DbSet<T>, IIncludableQueryable<T, TIncludesProperty>>> includesNavigationPropertyPathFunc);

        /// <summary>Returns a filtered and ordered page of entities.</summary>
        /// <typeparam name="TOrderProperty">The ordering property type.</typeparam>
        /// <param name="where">The filter expression.</param>
        /// <param name="orderBy">The ordering expression.</param>
        /// <param name="orderingType">The ordering direction.</param>
        /// <param name="pageIndex">The one-based page index.</param>
        /// <param name="pageCount">The maximum number of records per page.</param>
        /// <param name="includes">String navigation-property paths to include.</param>
        /// <returns>The matching page and its metadata.</returns>
        PagedList<T> GetObjectList<TOrderProperty>(Expression<Func<T, bool>> where, Expression<Func<T, TOrderProperty>> orderBy, OrderingType orderingType, int pageIndex, int pageCount, params string[] includes);

        /// <summary>Returns a filtered and ordered page using a typed include expression.</summary>
        /// <typeparam name="TOrderProperty">The ordering property type.</typeparam>
        /// <typeparam name="TIncludesProperty">The included navigation property type.</typeparam>
        /// <param name="where">The filter expression.</param>
        /// <param name="orderBy">The ordering expression.</param>
        /// <param name="orderingType">The ordering direction.</param>
        /// <param name="pageIndex">The one-based page index.</param>
        /// <param name="pageCount">The maximum number of records per page.</param>
        /// <param name="includesNavigationPropertyPathFunc">The typed include expression.</param>
        /// <returns>The matching page and its metadata.</returns>
        PagedList<T> GetObjectList<TOrderProperty, TIncludesProperty>(Expression<Func<T, bool>> where, Expression<Func<T, TOrderProperty>> orderBy, OrderingType orderingType, int pageIndex, int pageCount, Expression<Func<DbSet<T>, IIncludableQueryable<T, TIncludesProperty>>> includesNavigationPropertyPathFunc);

        /// <summary>Asynchronously returns a filtered and ordered page of entities.</summary>
        /// <typeparam name="TOrderProperty">The ordering property type.</typeparam>
        /// <param name="where">The filter expression.</param>
        /// <param name="orderBy">The ordering expression.</param>
        /// <param name="orderingType">The ordering direction.</param>
        /// <param name="pageIndex">The one-based page index.</param>
        /// <param name="pageCount">The maximum number of records per page.</param>
        /// <param name="includes">String navigation-property paths to include.</param>
        /// <returns>A task containing the matching page and its metadata.</returns>
        Task<PagedList<T>> GetObjectListAsync<TOrderProperty>(Expression<Func<T, bool>> where, Expression<Func<T, TOrderProperty>> orderBy, OrderingType orderingType, int pageIndex, int pageCount, params string[] includes);

        /// <summary>Asynchronously returns a filtered page using a typed include expression.</summary>
        /// <typeparam name="TOrderProperty">The ordering property type.</typeparam>
        /// <typeparam name="TIncludesProperty">The included navigation property type.</typeparam>
        /// <param name="where">The filter expression.</param>
        /// <param name="orderBy">The ordering expression.</param>
        /// <param name="orderingType">The ordering direction.</param>
        /// <param name="pageIndex">The one-based page index.</param>
        /// <param name="pageCount">The maximum number of records per page.</param>
        /// <param name="includesNavigationPropertyPathFunc">The typed include expression.</param>
        /// <returns>A task containing the matching page and its metadata.</returns>
        Task<PagedList<T>> GetObjectListAsync<TOrderProperty, TIncludesProperty>(Expression<Func<T, bool>> where, Expression<Func<T, TOrderProperty>> orderBy, OrderingType orderingType, int pageIndex, int pageCount, Expression<Func<DbSet<T>, IIncludableQueryable<T, TIncludesProperty>>> includesNavigationPropertyPathFunc);

        /// <summary>Returns the first matching entity or the default value.</summary>
        /// <param name="where">The filter expression.</param>
        /// <param name="includes">String navigation-property paths to include.</param>
        /// <returns>The first match, or <see langword="null"/>.</returns>
        T GetFirstOrDefaultObject(Expression<Func<T, bool>> where, params string[] includes);
        /// <summary>Returns the first matching entity using a typed include expression.</summary>
        /// <typeparam name="TIncludesProperty">The included navigation property type.</typeparam>
        /// <param name="where">The filter expression.</param>
        /// <param name="includesNavigationPropertyPathFunc">The typed include expression.</param>
        /// <returns>The first match, or <see langword="null"/>.</returns>
        T GetFirstOrDefaultObject<TIncludesProperty>(Expression<Func<T, bool>> where, Expression<Func<DbSet<T>, IIncludableQueryable<T, TIncludesProperty>>> includesNavigationPropertyPathFunc);
        /// <summary>Returns the first matching entity after applying ordering.</summary>
        /// <typeparam name="TOrderProperty">The ordering property type.</typeparam>
        /// <param name="where">The filter expression.</param>
        /// <param name="orderBy">The ordering expression.</param>
        /// <param name="orderingType">The ordering direction.</param>
        /// <param name="includes">String navigation-property paths to include.</param>
        /// <returns>The first match, or <see langword="null"/>.</returns>
        T GetFirstOrDefaultObject<TOrderProperty>(Expression<Func<T, bool>> where, Expression<Func<T, TOrderProperty>> orderBy, OrderingType orderingType, params string[] includes);
        /// <summary>Returns the first ordered match using a typed include expression.</summary>
        /// <typeparam name="TOrderProperty">The ordering property type.</typeparam>
        /// <typeparam name="TIncludesProperty">The included navigation property type.</typeparam>
        /// <param name="where">The filter expression.</param>
        /// <param name="orderBy">The ordering expression.</param>
        /// <param name="orderingType">The ordering direction.</param>
        /// <param name="includesNavigationPropertyPathFunc">The typed include expression.</param>
        /// <returns>The first match, or <see langword="null"/>.</returns>
        T GetFirstOrDefaultObject<TOrderProperty, TIncludesProperty>(Expression<Func<T, bool>> where, Expression<Func<T, TOrderProperty>> orderBy, OrderingType orderingType, Expression<Func<DbSet<T>, IIncludableQueryable<T, TIncludesProperty>>> includesNavigationPropertyPathFunc);

        /// <summary>Counts entities matching a predicate.</summary>
        /// <param name="where">The filter expression.</param>
        /// <param name="includes">String navigation-property paths to include.</param>
        /// <returns>The number of matching entities.</returns>
        int ObjectCount(Expression<Func<T, bool>> where, params string[] includes);
        /// <summary>Counts entities matching a predicate using a typed include expression.</summary>
        /// <typeparam name="TIncludesProperty">The included navigation property type.</typeparam>
        /// <param name="where">The filter expression.</param>
        /// <param name="includesNavigationPropertyPathFunc">The typed include expression.</param>
        /// <returns>The number of matching entities.</returns>
        int ObjectCount<TIncludesProperty>(Expression<Func<T, bool>> where, Expression<Func<DbSet<T>, IIncludableQueryable<T, TIncludesProperty>>> includesNavigationPropertyPathFunc);
        /// <summary>Calculates a decimal sum over matching entities.</summary>
        /// <param name="where">The filter expression.</param>
        /// <param name="sum">The decimal selector.</param>
        /// <param name="includes">String navigation-property paths to include.</param>
        /// <returns>The calculated sum.</returns>
        decimal GetSum(Expression<Func<T, bool>> where, Expression<Func<T, decimal>> sum, params string[] includes);
        /// <summary>Calculates a decimal sum using a typed include expression.</summary>
        /// <typeparam name="TIncludesProperty">The included navigation property type.</typeparam>
        /// <param name="where">The filter expression.</param>
        /// <param name="sum">The decimal selector.</param>
        /// <param name="includesNavigationPropertyPathFunc">The typed include expression.</param>
        /// <returns>The calculated sum.</returns>
        decimal GetSum<TIncludesProperty>(Expression<Func<T, bool>> where, Expression<Func<T, decimal>> sum, Expression<Func<DbSet<T>, IIncludableQueryable<T, TIncludesProperty>>> includesNavigationPropertyPathFunc);

        /// <summary>Adds an entity to the current unit of work.</summary>
        /// <param name="obj">The entity to add.</param>
        void Add(T obj);
        /// <summary>Marks an entity as modified in the current unit of work.</summary>
        /// <param name="obj">The entity to update.</param>
        void Update(T obj);
        /// <summary>Adds or updates an entity and saves the current unit of work.</summary>
        /// <param name="obj">The entity to persist.</param>
        void Save(T obj);
        /// <summary>Marks an entity for deletion.</summary>
        /// <param name="obj">The entity to delete.</param>
        /// <param name="softDelete">Whether to perform a soft delete when supported.</param>
        void Delete(T obj, bool softDelete = false);
        /// <summary>Persists the current unit of work synchronously.</summary>
        void SaveChanges();

        /// <summary>Asynchronously adds or updates an entity and saves the current unit of work.</summary>
        /// <param name="obj">The entity to persist.</param>
        /// <returns>A task that completes after persistence.</returns>
        Task SaveAsync(T obj);
        /// <summary>Persists selected entity properties asynchronously.</summary>
        /// <param name="obj">The entity to update.</param>
        /// <param name="propertyNames">The property names to persist.</param>
        Task SavePropertiesAsync(T obj, params string[] propertyNames);
        /// <summary>Deletes an entity asynchronously.</summary>
        /// <param name="obj">The entity to delete.</param>
        /// <param name="softDelete">Whether to perform a soft delete when supported.</param>
        /// <returns>A task that completes after deletion.</returns>
        Task DeleteAsync(T obj, bool softDelete = false);
        /// <summary>Persists the current unit of work asynchronously.</summary>
        Task SaveChangesAsync();

        /// <summary>Asynchronously returns the first entity matching a predicate.</summary>
        /// <param name="where">The filter expression.</param>
        /// <param name="includes">String navigation-property paths to include.</param>
        /// <returns>A task containing the first match, or <see langword="null"/>.</returns>
        Task<T> GetFirstOrDefaultObjectAsync(Expression<Func<T, bool>> where, params string[] includes);
        /// <summary>Asynchronously returns the first match using a typed include expression.</summary>
        /// <typeparam name="TIncludesProperty">The included navigation property type.</typeparam>
        /// <param name="where">The filter expression.</param>
        /// <param name="includesNavigationPropertyPathFunc">The typed include expression.</param>
        /// <returns>A task containing the first match, or <see langword="null"/>.</returns>
        Task<T> GetFirstOrDefaultObjectAsync<TIncludesProperty>(Expression<Func<T, bool>> where, Expression<Func<DbSet<T>, IIncludableQueryable<T, TIncludesProperty>>> includesNavigationPropertyPathFunc);
        /// <summary>Asynchronously returns the first ordered match.</summary>
        /// <typeparam name="TOrderProperty">The ordering property type.</typeparam>
        /// <param name="where">The filter expression.</param>
        /// <param name="orderBy">The ordering expression.</param>
        /// <param name="orderingType">The ordering direction.</param>
        /// <param name="includes">String navigation-property paths to include.</param>
        /// <returns>A task containing the first match, or <see langword="null"/>.</returns>
        Task<T> GetFirstOrDefaultObjectAsync<TOrderProperty>(Expression<Func<T, bool>> where, Expression<Func<T, TOrderProperty>> orderBy, OrderingType orderingType, params string[] includes);
        /// <summary>Asynchronously returns the first ordered match using a typed include expression.</summary>
        /// <typeparam name="TOrderProperty">The ordering property type.</typeparam>
        /// <typeparam name="TIncludesProperty">The included navigation property type.</typeparam>
        /// <param name="where">The filter expression.</param>
        /// <param name="orderBy">The ordering expression.</param>
        /// <param name="orderingType">The ordering direction.</param>
        /// <param name="includesNavigationPropertyPathFunc">The typed include expression.</param>
        /// <returns>A task containing the first match, or <see langword="null"/>.</returns>
        Task<T> GetFirstOrDefaultObjectAsync<TOrderProperty, TIncludesProperty>(Expression<Func<T, bool>> where, Expression<Func<T, TOrderProperty>> orderBy, OrderingType orderingType, Expression<Func<DbSet<T>, IIncludableQueryable<T, TIncludesProperty>>> includesNavigationPropertyPathFunc);
        /// <summary>Asynchronously counts entities matching a predicate.</summary>
        /// <param name="where">The filter expression.</param>
        /// <param name="includes">String navigation-property paths to include.</param>
        /// <returns>A task containing the match count.</returns>
        Task<int> ObjectCountAsync(Expression<Func<T, bool>> where, params string[] includes);
        /// <summary>Asynchronously counts entities using a typed include expression.</summary>
        /// <typeparam name="TIncludesProperty">The included navigation property type.</typeparam>
        /// <param name="where">The filter expression.</param>
        /// <param name="includesNavigationPropertyPathFunc">The typed include expression.</param>
        /// <returns>A task containing the match count.</returns>
        Task<int> ObjectCountAsync<TIncludesProperty>(Expression<Func<T, bool>> where, Expression<Func<DbSet<T>, IIncludableQueryable<T, TIncludesProperty>>> includesNavigationPropertyPathFunc);
        /// <summary>Asynchronously calculates a decimal sum.</summary>
        /// <param name="where">The filter expression.</param>
        /// <param name="sum">The decimal selector.</param>
        /// <param name="includes">String navigation-property paths to include.</param>
        /// <returns>A task containing the calculated sum.</returns>
        Task<decimal> GetSumAsync(Expression<Func<T, bool>> where, Expression<Func<T, decimal>> sum, params string[] includes);
        /// <summary>Asynchronously calculates a decimal sum using a typed include expression.</summary>
        /// <typeparam name="TIncludesProperty">The included navigation property type.</typeparam>
        /// <param name="where">The filter expression.</param>
        /// <param name="sum">The decimal selector.</param>
        /// <param name="includesNavigationPropertyPathFunc">The typed include expression.</param>
        /// <returns>A task containing the calculated sum.</returns>
        Task<decimal> GetSumAsync<TIncludesProperty>(Expression<Func<T, bool>> where, Expression<Func<T, decimal>> sum, Expression<Func<DbSet<T>, IIncludableQueryable<T, TIncludesProperty>>> includesNavigationPropertyPathFunc);

        /// <summary>Deletes a sequence of entities asynchronously.</summary>
        /// <param name="objs">The entities to delete.</param>
        /// <param name="deleteItemAction">An optional callback for each entity.</param>
        /// <param name="softDelete">Whether to perform soft deletes when supported.</param>
        /// <returns>A task that completes after deletion.</returns>
        Task DeleteAllAsync(IEnumerable<T> objs, Action<T> deleteItemAction = null, bool softDelete = false);
        /// <summary>Adds a sequence of entities asynchronously.</summary>
        /// <param name="objs">The entities to add.</param>
        /// <returns>A task that completes after the entities are persisted.</returns>
        Task AddAllAsync(IEnumerable<T> objs);
        /// <summary>Asynchronously returns a page using a textual order specification.</summary>
        /// <param name="where">The filter expression.</param>
        /// <param name="OrderbyField">The comma-separated order specification.</param>
        /// <param name="pageIndex">The one-based page index.</param>
        /// <param name="pageCount">The maximum number of records per page.</param>
        /// <param name="includes">String navigation-property paths to include.</param>
        /// <returns>A task containing the matching page.</returns>
        Task<PagedList<T>> GetObjectListAsync(Expression<Func<T, bool>> where, string OrderbyField, int pageIndex, int pageCount, params string[] includes);
        /// <summary>Asynchronously returns a page using a typed include expression.</summary>
        /// <typeparam name="TIncludesProperty">The included navigation property type.</typeparam>
        /// <param name="where">The filter expression.</param>
        /// <param name="OrderbyField">The comma-separated order specification.</param>
        /// <param name="pageIndex">The one-based page index.</param>
        /// <param name="pageCount">The maximum number of records per page.</param>
        /// <param name="includesNavigationPropertyPathFunc">The typed include expression.</param>
        /// <returns>A task containing the matching page.</returns>
        Task<PagedList<T>> GetObjectListAsync<TIncludesProperty>(Expression<Func<T, bool>> where, string OrderbyField, int pageIndex, int pageCount, Expression<Func<DbSet<T>, IIncludableQueryable<T, TIncludesProperty>>> includesNavigationPropertyPathFunc);
        /// <summary>Saves a sequence of entities asynchronously.</summary>
        /// <param name="objs">The entities to persist.</param>
        /// <returns>A task that completes after persistence.</returns>
        Task SaveObjectListAsync(IEnumerable<T> objs);
        /// <summary>Begins an asynchronous transaction using the provider default isolation level.</summary>
        Task BeginTransactionAsync();
        /// <summary>Begins an asynchronous transaction using a specific isolation level.</summary>
        /// <param name="isolationLevel">The requested transaction isolation level.</param>
        Task BeginTransactionAsync(System.Data.IsolationLevel isolationLevel);
        /// <summary>Begins a synchronous transaction using the provider default isolation level.</summary>
        void BeginTransaction();
        /// <summary>Begins a synchronous transaction using a specific isolation level.</summary>
        /// <param name="isolationLevel">The requested transaction isolation level.</param>
        void BeginTransaction(System.Data.IsolationLevel isolationLevel);
        /// <summary>Rolls back the current transaction.</summary>
        void RollbackTransaction();
        /// <summary>Commits the current transaction.</summary>
        void CommitTransaction();
    }
}
