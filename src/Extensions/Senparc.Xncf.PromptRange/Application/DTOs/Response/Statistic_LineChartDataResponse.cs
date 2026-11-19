/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Statistic_LineChartDataResponse.cs
    File description: Statistic_LineChartDataResponse related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.20.16 Localized PromptRange administration and runtime surfaces

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
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

            public decimal Z { get; set; }

            public PromptItemDto Data { get; set; }

            #endregion


            #region ctor

            public Point()
            {
            }

            public Point(string x, string y, decimal z, PromptItemDto data)
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