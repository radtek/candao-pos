﻿namespace Models.Response
{
    /// <summary>
    /// 获取分店信息返回类。
    /// </summary>
    public class GetBranchInfoResponse : JavaResponse
    {
        public BranchInfoData data { get; set; }
    }

    /// <summary>
    /// 分店信息数据类。
    /// </summary>
    public class BranchInfoData
    {
        public string branchaddress { get; set; }

        public string updatetime { get; set; }

        public string branchcode { get; set; }

        public string branchid { get; set; }

        public string serverversion { get; set; }

        public string id { get; set; }

        public string managername { get; set; }

        public string managerid { get; set; }

        public string managertel { get; set; }

        public string insertime { get; set; }

        public string tenantid { get; set; }

        public string branchname { get; set; }
    }
}