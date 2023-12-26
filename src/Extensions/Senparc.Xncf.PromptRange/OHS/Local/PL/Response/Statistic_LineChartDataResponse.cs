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
            public Point()
            {
            }

            public Point(int x, int y, int z, PromptItemDto data)
            {
                X = x;
                Y = y;
                Z = z;
                Data = data;
            }

            public int X { get; set; }
            public int Y { get; set; }
            public int Z { get; set; }

            public PromptItemDto Data { get; set; }
        }
    }
}