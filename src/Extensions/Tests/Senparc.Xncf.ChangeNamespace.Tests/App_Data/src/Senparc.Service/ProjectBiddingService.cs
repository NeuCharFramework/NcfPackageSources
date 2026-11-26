using Senparc.Core.Models;
using Senparc.Log;

namespace Senparc.Service
{
    public interface IProjectBiddingService : IBaseClientService<ProjectBidding>
    {
        /// <summary>Increments the developer's total project-bidding count.</summary>
        /// <param name="developerId">The developer identifier.</param>
        /// <param name="userName">The operator name recorded by the save operation.</param>
        void AddBiddingCount(int developerId, string userName);
        /// <summary>Increments successful bids and recalculates the developer level.</summary>
        /// <param name="developer">The developer whose counters are updated.</param>
        /// <param name="userName">The operator name recorded by the save operation.</param>
        void AddSuccessBiddingCount(Developer developer, string userName);
        /// <summary>Marks a project bid as finished.</summary>
        /// <param name="id">The project-bidding identifier.</param>
        void Finish(int id);
        /// <summary>Stores a grade and comment on a completed project bid.</summary>
        /// <param name="projectBidding">The bid being reviewed.</param>
        /// <param name="grade">The review grade.</param>
        /// <param name="comment">The review comment.</param>
        void AddComment(ProjectBidding projectBidding, int grade, string comment);
        /// <summary>Determines whether a developer has already bid on a project demand.</summary>
        /// <param name="developerId">The developer identifier.</param>
        /// <param name="projectDemandId">The project-demand identifier.</param>
        /// <returns><see langword="true"/> when a matching bid exists.</returns>
        bool ExistBidding(int developerId, int projectDemandId);
    }

    public class ProjectBiddingService : BaseClientService<ProjectBidding>, IProjectBiddingService
    {
        private IDeveloperService _developerService;
        public ProjectBiddingService(IProjectBiddingRepository ProjectBiddingRepo, IDeveloperService developerService)
            : base(ProjectBiddingRepo)
        {
            _developerService = developerService;
        }

        public void AddBiddingCount(int developerId, string userName)
        {
            var developer = _developerService.GetObject(developerId);
            developer.ProjectBiddingCount++;
            _developerService.SaveObject(developer, userName);
        }

        public void AddSuccessBiddingCount(Developer developer, string userName)
        {
            developer.SuccessProjectBiddingCount++;
            UpLever(developer);
            _developerService.SaveObject(developer, userName);
        }

        public void Finish(int id)
        {
            var projectBidding = GetObject(id);

            projectBidding.IsFinished = true;
            SaveObject(projectBidding);
        }

        public void AddComment(ProjectBidding projectBidding, int grade, string comment)
        {
            projectBidding.CommentGrade = grade;
            projectBidding.CommentText = comment;
            SaveObject(projectBidding);
        }

        /// <summary>
        /// Raises a developer's level when the successful-bid threshold for the
        /// current level has been exceeded.
        /// </summary>
        /// <param name="developer">The developer whose level is evaluated.</param>
        protected void UpLever(Developer developer)
        {
            var count = developer.SuccessProjectBiddingCount;
            var level = developer.Level;
            int sum = 0;
            for (int i = 0; i <= level; i++)
            {
                sum += (i + 1) * i / 2;
            }

            if (count > sum)
            {
                developer.Level += 1;
            }
        }

        public override void SaveObject(ProjectBidding obj)
        {
            var isInsert = base.IsInsert(obj);
            base.SaveObject(obj);
            LogUtility.WebLogger.InfoFormat("ProjectBidding{2}：{0}（ID：{1}）", obj.Developer.Name, obj.Id, isInsert ? "新增" : "编辑");
        }

        public override void DeleteObject(ProjectBidding obj)
        {
            LogUtility.WebLogger.InfoFormat("ProjectBidding被删除：{0}（ID：{1}）", obj.Developer.Name, obj.Id);
            base.DeleteObject(obj);
        }

        /// <summary>Determines whether a developer has already bid on a project demand.</summary>
        /// <param name="developerId">The developer identifier.</param>
        /// <param name="projectDemandId">The project-demand identifier.</param>
        /// <returns><see langword="true"/> when a matching bid exists.</returns>
        public bool ExistBidding(int developerId, int projectDemandId)
        {
            return this.GetObject(z => z.DeveloperId == developerId && z.ProjectDemandId == projectDemandId) != null;
        }
    }
}

