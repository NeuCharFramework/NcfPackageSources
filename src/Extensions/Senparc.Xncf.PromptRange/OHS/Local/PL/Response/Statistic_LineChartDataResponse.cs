using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto;

namespace Senparc.Xncf.PromptRange.OHS.Local.PL.Response
{
    public class Statistic_TodayTacticResponse
    {
        public string RangeName { get; set; }

        public DateTime QueryTime { get; set; }

        /// <summary>
        /// x,y,z
        /// 
        /// </summary>
        public List<List<Point>> DataPoints { get; set; }

        public Statistic_TodayTacticResponse(string rangeName, DateTime queryTime)
        {
            RangeName = rangeName;
            QueryTime = queryTime;
            DataPoints = new();
        }

        public class Point
        {
            #region property

            public string X { get; set; }

            public string Y { get; set; }

            public int Z { get; set; }

            public PromptItemDto Data { get; set; }

            #endregion


            #region ctor

            public Point()
            {
            }

            public Point(string x, string y, int z, PromptItemDto data)
            {
                X = x;
                Y = y;
                Z = z;
                Data = data;
            }

            #endregion
        }
    }
}