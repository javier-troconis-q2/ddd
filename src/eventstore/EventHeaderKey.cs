﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace eventstore
{
	//todo:maybe use prefix to filterout system event header entries ?
    internal static class EventHeaderKey
    {
	    public const string Topics = "topics";
		public const string CorrelationId = "correlationId";
	}
}
