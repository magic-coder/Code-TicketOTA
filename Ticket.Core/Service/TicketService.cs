﻿using FengjingSDK461.Helpers;
using FengjingSDK461.Model.Request;
using FengjingSDK461.Model.Response;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using Ticket.Core.Repository;
using Ticket.SqlSugar.Models;
using Ticket.Model.Docom;
using Ticket.Model.Enum;
using Ticket.Model.Model.TravelAgency;
using Ticket.Model.Result;
using Ticket.Utility.Extensions;

namespace Ticket.Core.Service
{
    /// <summary>
    /// 门票
    /// </summary>
    public class TicketService
    {
        private readonly TicketRepository _ticketRepository;
        private readonly TicketRelationRepository _ticketRelationRepository;
        private readonly TicketTypeRepository _ticketTypeRepository;
        private readonly TicketRuleRepository _ticketRuleRepository;
        private readonly OtaTicketRelationService _otaTicketRelationService;
        private readonly OtaTicketRelationRepository _otaTicketRelationRepository;

        public TicketService(
            TicketRepository ticketRepository,
            TicketRelationRepository ticketRelationRepository,
            TicketTypeRepository ticketTypeRepository,
            TicketRuleRepository ticketRuleRepository,
            OtaTicketRelationService otaTicketRelationService,
            OtaTicketRelationRepository otaTicketRelationRepository)
        {
            _ticketRepository = ticketRepository;
            _ticketRelationRepository = ticketRelationRepository;
            _ticketTypeRepository = ticketTypeRepository;
            _ticketRuleRepository = ticketRuleRepository;
            _otaTicketRelationService = otaTicketRelationService;
            _otaTicketRelationRepository = otaTicketRelationRepository;
        }

        /// <summary>
        /// 获取门票(产品)详情
        /// </summary>
        /// <param name="ticketId"></param>
        /// <returns></returns>
        public Tbl_Ticket Get(int ticketId)
        {
            return _ticketRepository.FirstOrDefault(a => a.TicketId == ticketId);
        }

        /// <summary>
        /// 获取门票(产品)详情
        /// </summary>
        /// <param name="ticketId"></param>
        /// <returns></returns>
        public List<Tbl_Ticket> GetList(List<int> ticketIds)
        {
            return _ticketRepository.GetAllList(a => ticketIds.Contains(a.TicketId));
        }

        /// <summary>
        /// 获取分销商门票(产品)
        /// </summary>
        /// <param name="codes">分销商产品编码集合</param>
        /// <returns></returns>
        public List<Tbl_Ticket> GetListByBusiness(List<string> codes)
        {
            return _ticketRepository.GetAllList(a => codes.Contains(a.Code) && a.TicketSource == 2);
        }

        /// <summary>
        /// 获取自己景区的所有团队票的门票(产品)列表
        /// </summary>
        /// <param name="ticketIds">门票id集合</param>
        /// <param name="scenicId">景区</param>
        /// <returns></returns>
        public List<Tbl_Ticket> GetPageList(List<int> ticketIds, int scenicId, int pageSize, int pageIndex, out int total)
        {
            //过滤掉已过期的产品
            DateTime nowTime = DateTime.Now.Date;
            DateTime endTime = nowTime.AddDays(-1);

            var infoList = _ticketRepository.GetPageList(
                pageSize,
                pageIndex,
                out total,
                p => ticketIds.Contains(p.TicketId) &&
                    p.ScenicId == scenicId &&
                    p.TicketSource == 2 &&
                    p.ExpiryDateStart <= nowTime &&
                    p.ExpiryDateEnd >= nowTime,
            a => a.CreateTime, false);
            return infoList;
        }

        /// <summary>
        /// 获取自己景区的所有团队票的门票(产品)列表
        /// </summary>
        /// <param name="ticketIds">门票id集合</param>
        /// <param name="scenicId">景区</param>
        /// <returns></returns>
        public List<Tbl_Ticket> GetTickets(List<int> ticketIds, int scenicId)
        {
            //过滤掉已过期的产品
            DateTime nowTime = DateTime.Now.Date;

            var infoList = _ticketRepository.GetAllList(p =>
            ticketIds.Contains(p.TicketId) &&
            p.ScenicId == scenicId &&
            p.TicketSource == 2 &&
            p.ExpiryDateStart <= nowTime &&
            p.ExpiryDateEnd >= nowTime
            ).OrderByDescending(a => a.CreateTime).ToList();
            return infoList;
        }

        /// <summary>
        /// 获取自己景区的所有团队票的门票(产品)列表
        /// </summary>
        /// <param name="ticketIds">门票id集合</param>
        /// <param name="scenicId">景区</param>
        /// <param name="playTime">游玩时间</param>
        /// <returns></returns>
        public List<Tbl_Ticket> GetTickets(List<int> ticketIds, int scenicId, DateTime playTime)
        {
            //过滤掉已过期的产品
            DateTime nowTime = DateTime.Now.Date;

            //且游玩日期在有效期内
            DateTime endPayT = playTime.Date;

            var infoList = _ticketRepository.GetAllList(p =>
            ticketIds.Contains(p.TicketId) &&
            p.ScenicId == scenicId &&
            p.TicketSource == 2 &&
            p.ExpiryDateStart <= nowTime &&
             p.ExpiryDateEnd >= nowTime &&
             p.ExpiryDateStart <= endPayT &&
             p.ExpiryDateEnd >= endPayT
            ).OrderByDescending(a => a.CreateTime).ToList();
            return infoList;
        }

        /// <summary>
        /// 获取自己景区的门票(产品)列表
        /// </summary>
        /// <param name="ticketId">门票id</param>
        /// <param name="scenicId">景区</param>
        /// <param name="playTime">游玩时间</param>
        /// <returns></returns>
        public Tbl_Ticket GetTicket(int ticketId, int scenicId, DateTime playTime)
        {
            //过滤掉已过期的产品
            DateTime nowTime = DateTime.Now.Date;

            //且游玩日期在有效期内
            DateTime endPayT = playTime.Date;

            var ticket = _ticketRepository.FirstOrDefault(p =>
             p.TicketId == ticketId &&
             p.ScenicId == scenicId &&
             p.TicketSource == 2 &&
             p.ExpiryDateStart <= nowTime &&
             p.ExpiryDateEnd >= nowTime &&
             p.ExpiryDateStart <= endPayT &&
             p.ExpiryDateEnd >= endPayT
            );
            if (ticket == null)
            {
                return null;
            }

            //根据游玩日期，变动价格
            //根据orderId 顺序排列 取第一条
            var ticketRelation = _ticketRelationRepository.GetAll().Where(p =>
            p.ExpiryDateStart <= nowTime &&
            p.ExpiryDateEnd >= nowTime &&
            p.ScenicId == ticket.ScenicId &&
            p.TicketId == ticket.TicketId).OrderBy(c => c.OrderId).First();
            if (ticketRelation != null && ticketRelation.SalePrice > 0)
            {
                ticket.SalePrice = ticketRelation.SalePrice;
                ticket.MarkPrice = ticketRelation.MarkPrice;
            }
            return ticket;
        }


        public List<Tbl_Ticket> GetTickets(List<int> list)
        {
            var tickets = _ticketRepository.GetAllList(a => list.Contains(a.TicketId));
            return tickets;
        }

        /// <summary>
        /// 获取票的信息
        /// </summary>
        /// <param name="validityDate">游玩日期</param>
        /// <param name="ticketId"></param>
        /// <returns></returns>
        public Tbl_Ticket GetTicket(DateTime validityDate, int ticketId)
        {
            //根据游玩日期，变动价格
            var whereR = PredicateBuilder.True<Tbl_TicketRelation>();
            if (validityDate != DateTime.MinValue)
            {
                DateTime endPay = validityDate.AddDays(-1);
                whereR = PredicateBuilder.And(@whereR, p =>
                p.ExpiryDateStart <= validityDate &&
                p.ExpiryDateEnd >= validityDate);
            }
            var ticket = _ticketRepository.FirstOrDefault(o => o.TicketId == ticketId);
            if (ticket == null)
            {
                return null;
            }
            //根据游玩日期，变动价格
            whereR = PredicateBuilder.And(@whereR, p => p.ScenicId == ticket.ScenicId);
            whereR = PredicateBuilder.And(@whereR, p => p.TicketId == ticket.TicketId);

            //根据orderId 顺序排列 取第一条
            var ticketRelation = _ticketRelationRepository.GetAll().Where(whereR).OrderBy(c => c.OrderId).First();
            if (ticketRelation != null && ticketRelation.SalePrice > 0)
            {
                ticket.SalePrice = ticketRelation.SalePrice;
            }
            return ticket;
        }

        /// <summary>
        /// 更新票的日售票数
        /// </summary>
        /// <param name="tickets"></param>
        /// <param name="orderDetails"></param>
        public void UpdateTicketBySellCount(List<Tbl_Ticket> tickets, List<Tbl_OrderDetail> orderDetails)
        {
            List<Tbl_Ticket> tbl_Ticket = new List<Tbl_Ticket>();
            foreach (var item in orderDetails)
            {
                var ticket = tickets.FirstOrDefault(o => o.TicketId == item.TicketId);
                if (ticket != null)
                {
                    ticket.SellCount = ticket.SellCount.HasValue ? ticket.SellCount + item.Quantity : item.Quantity;
                }
            }
        }

        /// <summary>
        /// 更新票的日售票数
        /// </summary>
        /// <param name="tickets"></param>
        /// <param name="orderDetails"></param>
        public void UpdateTicketBySellCount(Tbl_Ticket ticket, Tbl_OrderDetail orderDetail)
        {
            //日销售限额为空或者0  为不限制
            if (ticket.StockCount.HasValue && ticket.StockCount.Value > 0)
            {
                if (ticket.LastUpdateTime.HasValue && ticket.LastUpdateTime.Value.Date == DateTime.Now.Date)
                {
                    ticket.SellCount = ticket.SellCount.HasValue ? ticket.SellCount + orderDetail.Quantity : 1;
                }
                ticket.LastUpdateTime = DateTime.Now;
                ticket.SellCount = 1;
                _ticketRepository.Update(ticket);
            }

        }

        /// <summary>
        /// 更新票的日售票数
        /// </summary>
        /// <param name="orderDetails"></param>
        public void UpdateTicketBySellCount(Tbl_OrderDetail tbl_OrderDetail)
        {
            var tbl_Ticket = _ticketRepository.FirstOrDefault(a => a.TicketId == tbl_OrderDetail.TicketId);
            if (tbl_Ticket != null)
            {
                int? selCount = 0;
                if (tbl_Ticket.SellCount > tbl_OrderDetail.Quantity)
                {
                    selCount = tbl_Ticket.SellCount - tbl_OrderDetail.Quantity;
                }
                tbl_Ticket.SellCount = tbl_Ticket.SellCount.HasValue ? selCount : 0;
                _ticketRepository.Update(tbl_Ticket);
            }
        }

        /// <summary>
        /// 更新日售票数
        /// </summary>
        /// <param name="tickets"></param>
        /// <param name="connection"></param>
        /// <param name="transaction"></param>
        public void UpdateTicket(List<Tbl_Ticket> tickets, SqlConnection connection, SqlTransaction transaction)
        {
            _ticketRepository.UpdateTicket(tickets, connection, transaction);
        }

        /// <summary>
        /// 更新日售票数
        /// </summary>
        /// <param name="tickets"></param>
        /// <param name="connection"></param>
        /// <param name="transaction"></param>
        public void UpdateTicket(List<Tbl_Ticket> tickets)
        {
            foreach (var row in tickets)
            {
                var count = 0;
                if (row.LastUpdateTime.HasValue && row.LastUpdateTime.Value.Date == DateTime.Now.Date)
                {
                    row.LastUpdateTime = DateTime.Now;
                    count = 1;
                }
                if (row.StockCount.HasValue)
                {
                    row.StockCount = row.StockCount.Value + count;
                }
                else
                {
                    row.StockCount = 0;
                }
                _ticketRepository.Update(row);
            }

        }

        /// <summary>
        /// 验证单个订单数据
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public DataValidResult ValidDataForOrderSingleCreateRequest(
            OrderSingleCreateRequest request,
            Tbl_OTABusiness business,
            Tbl_Ticket tbl_Ticket,
            OrderSingleCreateResponse response)
        {
            var result = new DataValidResult { Status = false };
            var ticket = request.Body.OrderInfo.Ticket;
            if (tbl_Ticket == null)
            {
                result.Code = "113019";
                result.Message = "创建订单失败，选择的游玩日期超出选购产品的有效期或者选购的产品无效";
                return result;
            }
            var existence = _otaTicketRelationService.CheckIsTicketId(business.Id, tbl_Ticket.TicketId);
            if (!existence)
            {
                result.Code = "113019";
                result.Message = "创建订单失败，选择的游玩日期超出选购产品的有效期或者选购的产品无效";
                return result;
            }
            if (tbl_Ticket.SalePrice != ticket.SellPrice)
            {
                result.Code = "113020";
                result.Message = "创建订单失败，价格不一致";
                return result;
            }
            //日销售限额为空或者0  为不限制
            if (tbl_Ticket.StockCount.HasValue && tbl_Ticket.StockCount.Value > 0)
            {
                if (!tbl_Ticket.LastUpdateTime.HasValue || tbl_Ticket.LastUpdateTime.Value.Date != DateTime.Now.Date)
                {
                    tbl_Ticket.SellCount = 0;
                }
                var sellCount = tbl_Ticket.SellCount.HasValue ? tbl_Ticket.SellCount.Value : 0;//库存
                response.Body.Inventory = tbl_Ticket.StockCount.Value - sellCount;
                if (sellCount + ticket.Quantity > tbl_Ticket.StockCount.Value)
                {
                    //开启了库存限制，购买数量超过了库存
                    result.Code = "113026";
                    result.Message = "创建订单失败，库存不足";
                    return result;
                }
            }
            else
            {
                response.Body.Inventory = 50000;
            }

            result.Status = true;
            return result;
        }





        /// <summary>
        /// 验证产品是否有效
        /// </summary>
        /// <param name="ticketIds"></param>
        /// <param name="scenicId"></param>
        /// <param name="playTime"></param>
        /// <returns>List<Tbl_Ticket></returns>
        public List<Tbl_Ticket> CheckIsTicketIds(List<int> ticketIds, int scenicId, DateTime playTime)
        {
            var tbl_Tickets = GetTickets(ticketIds, scenicId, playTime);
            if (ticketIds.Count != tbl_Tickets.Count)
            {
                return new List<Tbl_Ticket>();
            }
            DateTime endPay = playTime.AddDays(-1);
            foreach (var row in tbl_Tickets)
            {
                //根据游玩日期，变动价格
                //根据orderId 顺序排列 取第一条
                var ticketRelation = _ticketRelationRepository.GetAll().Where(p =>
                p.ExpiryDateStart <= playTime &&
                p.ExpiryDateEnd >= playTime &&
                p.ScenicId == row.ScenicId &&
                p.TicketId == row.TicketId).OrderBy(c => c.OrderId).First();
                if (ticketRelation != null && ticketRelation.SalePrice > 0)
                {
                    row.SalePrice = ticketRelation.SalePrice;
                    row.MarkPrice = ticketRelation.MarkPrice;
                }
            }
            return tbl_Tickets;
        }

        /// <summary>
        /// 验证基础数据
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public DataValidResult ValidDataForProductQueryRequest(ProductQueryRequest request)
        {
            var result = new DataValidResult { Status = false };
            if (request.Body.Type > 2 || request.Body.Type < 0)
            {
                result.Code = "111002";
                result.Message = "获取产品异常，分页的形式超出范围";
                return result;
            }
            if (request.Body.Type == 2 && request.Body.ProductId <= 0)
            {
                result.Code = "111003";
                result.Message = "获取产品异常，产品id不合法";
                return result;
            }
            if (request.Body.Type == 1 && request.Body.PageSize <= 0)
            {
                result.Code = "111004";
                result.Message = "获取产品异常，每页记录数不能小于1";
                return result;
            }
            if (request.Body.Type == 1 && request.Body.CurrentPage <= 0)
            {
                result.Code = "111004";
                result.Message = "获取产品异常，当前页数不能小于1";
                return result;
            }
            result.Status = true;
            return result;
        }

        /// <summary>
        /// 检查Ticket票在当前闸机是否允许检票
        /// </summary>
        /// <param name="ticketId"></param>
        /// <returns></returns>
        public bool CheckTicketIsDoorGate(int ticketId, string doorGateNo)
        {
            var ticket = Get(ticketId);
            if (ticket == null)
            {
                return false;
            }
            if (ticket.CheckWay == (int)TicketCheckWayType.AllNotPass)
            {
                return false;
            }
            if (ticket.CheckWay == (int)TicketCheckWayType.SpecifyTurnstile)
            {

            }
            return true;
        }

        /// <summary>
        /// 获取门票列表--扫一扫微信支付
        /// </summary>
        /// <param name="scenicId">景区id</param>
        /// <param name="playTime">游玩日期</param>
        /// <returns></returns>
        public List<TVMTicketItem> GetScanTicketList(int scenicId, DateTime playTime)
        {
            //过滤掉已过期的产品
            DateTime nowTime = DateTime.Now.Date;

            var tbl_TicketTypeList = _ticketTypeRepository.GetAllList(a => (a.TicketType == (int)TicketFirstType.散客票 || a.TicketType == (int)TicketFirstType.团体票) && a.ScenicId == scenicId).ToList();
            if (tbl_TicketTypeList.Count <= 0)
            {
                return new List<TVMTicketItem>();
            }
            var typeIds = tbl_TicketTypeList.Select(a => a.Id).ToList();
            var shelvesChannelType = ((int)ShelvesChannelEnum.MobileTicket).ToString();//上线渠道
            var ticketList = _ticketRepository.GetAllList(p =>
                p.ScenicId == scenicId &&
                p.ShelvesChannel.Contains(shelvesChannelType) &&
                typeIds.Contains(p.TypeId) &&
                (p.DataStatus & (int)TicketDataStatus.IsStop) == 0 &&
                p.TicketSource == 1 &&
                p.ExpiryDateStart <= nowTime &&
                p.ExpiryDateEnd >= nowTime);

            var ticketIds = ticketList.Select(a => a.TicketId).ToList();
            var Tbl_TicketRelations = _ticketRelationRepository.GetAllList(p => p.ScenicId == scenicId && ticketIds.Any(a => a == p.TicketId));

            DateTime endPay = playTime.AddDays(-1);
            //根据游玩日期，变动价格
            foreach (var item in ticketList)
            {
                //根据orderId 顺序排列 取第一条
                var tbl_TicketRelation = Tbl_TicketRelations.Where(p =>
                    p.TicketId == item.TicketId &&
                    p.Type == (int)TicketRelationEnum.TimeSlot &&
                    p.ExpiryDateStart <= nowTime &&
                    p.ExpiryDateEnd >= nowTime
                    ).OrderBy(c => c.OrderId).FirstOrDefault();

                if (tbl_TicketRelation != null)
                {
                    item.SalePrice = tbl_TicketRelation.SalePrice;
                    continue;
                }
                //特殊时间段价格表为空 同时 游玩日期 是周末则启动周末价格策略
                var dayOfWeek = playTime.DayOfWeek;
                if (dayOfWeek != DayOfWeek.Saturday && dayOfWeek != DayOfWeek.Sunday)
                {
                    continue;
                }
                var ticketRelation = Tbl_TicketRelations.FirstOrDefault(p => p.TicketId == item.TicketId && p.Type == (int)TicketRelationEnum.Weekend);
                if (ticketRelation != null)
                {
                    item.SalePrice = ticketRelation.SalePrice;
                }
            }

            var ruleIds = ticketList.Select(a => a.RuleId).Distinct().ToList();
            var ticketRuleList = _ticketRuleRepository.GetAllList(a => ruleIds.Contains(a.Id));
            var scanTicketList = new List<TVMTicketItem>();
            foreach (var row in ticketList)
            {
                var ticketRule = ticketRuleList.FirstOrDefault(a => a.Id == row.RuleId);
                if (ticketRule == null)
                {
                    continue;
                }
                if (ticketRule.CanBookAdvance)
                {
                    var playDataTime = playTime.AddHours(nowTime.Hour).AddMinutes(nowTime.Minute).AddSeconds(nowTime.Second);
                    var nowDataTime = nowTime.Date.AddHours(nowTime.Hour).AddMinutes(nowTime.Minute).AddSeconds(nowTime.Second);
                    var time = nowDataTime.AddDays(ticketRule.BookDay ?? 0).AddHours(ticketRule.BookHour ?? 0).AddMinutes(ticketRule.BookMinute ?? 0);
                    if (time > playDataTime)
                    {
                        continue;
                    }
                }
                scanTicketList.Add(new TVMTicketItem
                {
                    TicketId = row.TicketId,
                    TicketName = row.TicketName,
                    TicketPrice = row.SalePrice,
                    MinCount = row.MinOQ,
                    MaxCount = row.MaxOQ
                });
            }
            return scanTicketList;
        }

        /// <summary>
        /// 检查
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public TVMTicketQueryData CheckTVMTicketQueryDataRequest(string data)
        {
            try
            {
                var request = JsonHelper.JsonToObject<TVMTicketQueryData>(data);
                if (request.playTime == null)
                {
                    return null;
                }
                return request;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 获取旅行社可用产品列表
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public TPageResult<TicketViewModel> GetPageList(TicketQueryModel model)
        {
            return _ticketRepository.GetPageList(model);
        }
    }
}
