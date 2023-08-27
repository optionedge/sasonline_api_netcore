using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SASOnline.Api.NetCore
{
    public class MarketData
    {
        public int Precision;
        public int multiplier;
        public int BidPrice;
        public int BidQty;
        public int AskPrice;
        public int AskQty;
        public int Exchange;
        public char[] TradingSymbol;
        public int InstrumentCode;
        public int LastTradePrice;
        public int LastTradeQuantity;
        public int ExchangeTimestamp;
        public int LastTradeTime;
        public long LowDpr;
        public long HighDpr;
        public int OpenPrice;
        public int ClosePrice;
        public int HighPrice;
        public int LowPrice;
        public long TotalBuyQty;
        public long TotalSellQty;
        public int YearlyHigh;
        public int YearlyLow;
        public int AvgTradePrice;
        public int CurrentOpenInterest;
        public int InitialOpenInterest;
        public int ChangeOpenInterest;
        public int TradeVolume;
    }
}
