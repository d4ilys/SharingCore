using Daily.SharingCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;

namespace FreeSharding.SeparateDatabase
{
    public class SeparateDatabase
    {
        /// <summary>
        /// 输入一个日期范围，根据跨月返回相应的数据
        /// </summary>
        public static SeparateDatabaseResult SparateInfo(Action<SeparateDatabaseParam> action)
        {
            var param = new SeparateDatabaseParam();
            action.Invoke(param);

            param.EndTime = param.EndTime;
            var startTime = param.StartTime;
            var startYear = startTime.Year;

            var endTime = param.EndTime;
            var endYear = param.EndTime.Year;

            //几年分一次
            var strideYear = param.StrideYear;
            var result = new SeparateDatabaseResult();
            if (startYear > endYear)
            {
                result.Code = 1; //日期错误
                result.Message = "开始日期不能大于结束日期";
                return result;
            }

            //得到基础信息
            var resultList = new List<YearList>();
            var yearList = new List<int>();
            do
            {
                yearList.Add(startYear);
                startYear += param.StrideYear;
            } while (startYear <= endYear);

            //对得到的日期进行正向排序
            var orderYearList = yearList.OrderBy(i => i).ToList();

            foreach (var i in orderYearList)
            {
                switch (orderYearList.Count())
                {
                    //不跨库
                    case 1:
                        var tempstart = startTime;
                        resultList.Add(new YearList()
                        {
                            Year = i,
                            SelectTime = new SelectTime
                            {
                                SelectStartTime = tempstart, SelectEndTime = endTime,
                            }
                        });
                        break;
                    //跨库
                    default:
                    {
                        //是否还要查询下一年的数据，如果有本年的结束日期是最后一天
                        if (orderYearList.Where(j => j > i).Any())
                        {
                            resultList.Add(new YearList()
                            {
                                Year = i,
                                SelectTime = new SelectTime
                                {
                                    SelectStartTime = startTime,
                                    SelectEndTime = new DateTime(i, 12, 31).AddDays(1),
                                }
                            });
                        }
                        else
                        {
                            //最后一年的结束日期就是用户输入的结束日期
                            resultList.Add(new YearList()
                            {
                                Year = i,
                                SelectTime = new SelectTime
                                {
                                    SelectStartTime = startTime, SelectEndTime = endTime,
                                }
                            });
                        }

                        break;
                    }
                }
            }

            if (resultList.Any())
            {
                result.Code = 0;
                result.YearTimeData = resultList;
                result.Message = "需要跨库处理";
            }

            return result;
        }
    }
}