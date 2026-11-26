using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Senparc.Core.Enums;
using Senparc.Core.Models;
using Senparc.Repository;
using Senparc.Log;

namespace Senparc.Service
{
    public interface IProjectKindService : IBaseClientService<ProjectKind>
    {
        /// <summary>Builds a parent-to-children project-kind tree.</summary>
        Dictionary<ProjectKind, List<ProjectKind>> GetKindTree();
        /// <summary>Increments the demand count for a project kind.</summary>
        /// <param name="kindId">The project-kind identifier.</param>
        void ProjectDemandCount(int kindId);
        /// <summary>Decrements the demand count for a project kind.</summary>
        /// <param name="kindId">The project-kind identifier.</param>
        void ReduceProjectDemandCount(int kindId);
        /// <summary>Creates missing first-level kinds from the ProjectKind_List enum.</summary>
        void CreateOneLevelKind();
        /// <summary>Creates missing second-level kinds under a named parent.</summary>
        /// <param name="names">The child kind names.</param>
        /// <param name="parentName">The parent enum value.</param>
        void CreateTwoLevelKind(string[] names, ProjectKind_List parentName);
        /// <summary>Returns visible first-level project kinds in descending identifier order.</summary>
        /// <returns>The visible parent kinds.</returns>
        List<ProjectKind> GetParentProjectKindList();
    }

    public class ProjectKindService : BaseClientService<ProjectKind>, IProjectKindService
    {
        public ProjectKindService(IProjectKindRepository projectKindRepo)
            : base(projectKindRepo)
        {

        }

        /// <inheritdoc />
        public Dictionary<ProjectKind, List<ProjectKind>> GetKindTree()
        {
            Dictionary<ProjectKind, List<ProjectKind>> result =
                new Dictionary<ProjectKind, List<ProjectKind>>();
            var fullList = base.GetFullList(z => true, z => z.Id, OrderingType.Ascending);

            foreach (var topKind in fullList.Where(z => z.Level == 0).OrderBy(z => z.DisplayOrder))
            {
                var subKinds = fullList.Where(z => z.ParentId == topKind.Id).OrderBy(z => z.DisplayOrder).ToList();
                result[topKind] = subKinds;
            }

            return result;
        }

        /// <summary>
        /// Gets child kinds for a parent identifier, or top-level kinds when no
        /// parent identifier is supplied.
        /// </summary>
        /// <param name="id">Optional parent kind identifier.</param>
        /// <returns>The matching project kinds.</returns>
        public List<ProjectKind> GetKindList(int? id)
        {
            var KindList = new List<ProjectKind>();

            if (id.HasValue)
            {
                KindList = GetFullList(z => z.ParentId == id, z => z.Id, OrderingType.Descending);
            }
            else
            {
                KindList = GetFullList(z => z.Level == 0, z => z.Id, OrderingType.Descending);
            }

            return KindList;
        }

        /// <inheritdoc />
        public void ProjectDemandCount(int kindId)
        {
            var projectKind = GetObject(kindId);
            projectKind.ProjectDemandCount++;
            SaveObject(projectKind);
        }

        /// <inheritdoc />
        public void ReduceProjectDemandCount(int kindId)
        {
            var projectKind = GetObject(kindId);
            projectKind.ProjectDemandCount--;
            SaveObject(projectKind);
        }

        /// <inheritdoc />
        public void CreateOneLevelKind()
        {
            string[] names = Enum.GetNames(typeof(ProjectKind_List));
            for (int i = 0; i < names.Count(); i++)
            {
                var projectKind = new ProjectKind()
                {
                    Name = names[i],
                    Description = names[i],
                    ProjectDemandCount = 0,
                    Level = 0,
                    DisplayOrder = 0,
                    Show = true,
                    AddTime = DateTime.Now
                };
                var name = names[i];
                var notExist = GetObject(z => z.Name == name) == null;
                if (notExist)
                {
                    SaveObject(projectKind);
                }
            }
        }

        /// <inheritdoc />
        public void CreateTwoLevelKind(string[] names, ProjectKind_List parentName)
        {
            var parent = parentName.ToString();
            var parentId = GetObject(z => z.Name == parent).Id;

            for (int i = 0; i < names.Count(); i++)
            {
                var projectKind = new ProjectKind()
                {
                    Name = names[i],
                    Description = names[i],
                    ParentId = parentId,
                    ProjectDemandCount = 0,
                    Level = 1,
                    DisplayOrder = 0,
                    Show = true,
                    AddTime = DateTime.Now
                };

                var name = names[i];
                var notExist = GetObject(z => z.Name == name) == null;
                if (notExist)
                {
                    SaveObject(projectKind);
                }
            }
        }

        public override void SaveObject(ProjectKind obj)
        {
            var isInsert = base.IsInsert(obj);
            base.SaveObject(obj);
            LogUtility.WebLogger.InfoFormat("ProjectKind{2}：{0}（ID：{1}）", obj.Name, obj.Id, isInsert ? "新增" : "编辑");
        }
        public override void DeleteObject(ProjectKind obj)
        {
            LogUtility.WebLogger.InfoFormat("ProjectKind被删除：{0}（ID：{1}）", obj.Name, obj.Id);
            base.DeleteObject(obj);
        }

        /// <inheritdoc />
        public List<ProjectKind> GetParentProjectKindList()
        {
            return GetFullList(z => z.Level == 0 && z.Show, z => z.Id, OrderingType.Descending);
        }
    }
}

