﻿using System.Collections.Generic;
using System.Linq;
using Ticket.SqlSugar.Models;
using Ticket.SqlSugar.Repository;

namespace Ticket.Core.Repository
{
    /// <summary>
    /// 指定闸机关联表
    /// </summary>
    public class TicketDoorGateRepository : RepositoryBase<Tbl_TicketDoorGate>
    {
        public List<int> FindByDoorGateIds(int ticketId)
        {
            return GetAllList(a => a.TicketId == ticketId).Select(a => a.DoorGateId).ToList();
        }
    }
}
