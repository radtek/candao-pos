﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CanDaoCD.Pos.Common.Operates
{
    /// <summary>
    /// 检查格式
    /// </summary>
   public class OCheckFormat
    {
       /// <summary>
       /// 检查手机号码正确
       /// </summary>
        /// <param name="phoneNum"></param>
       /// <returns></returns>
       public static bool IsMobilePhone(string phoneNum)
       {
           if (phoneNum == null)
           {
               return false;
           }
           return System.Text.RegularExpressions.Regex.IsMatch(phoneNum, @"^[1]+[3,5]+\d{9}");

       }
    }
}