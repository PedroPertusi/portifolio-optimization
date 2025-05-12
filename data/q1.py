import yfinance as yf

tickers = [
    "AAPL","AMGN","AMZN","AXP","BA","CAT","CRM","CSCO","CVX","DIS",
    "GS","HD","HON","IBM","JNJ","JPM","KO","MCD","MMM","MRK",
    "MSFT","NKE","NVDA","PG","SHW","TRV","UNH","V","VZ","WMT"
]

start_date = "2025-01-01"
end_date = "2025-03-31"

raw_data = yf.download(tickers, start=start_date, end=end_date)

close_data = raw_data["Close"]

close_data.to_csv("dow_jones_q1.csv")