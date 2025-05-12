namespace Library.Func

open System

/// Pure helper functions for portfolio returns and risk metrics.
module Helper =

  /// Given an array of prices [p0; p1; …; pn],
  /// returns [r1; …; rn] where rt = pt / p(t-1) - 1
  let dailyReturns (prices: float[]) : float[] =
    prices
    |> Array.pairwise
    |> Array.map (fun (pPrev, pNow) -> pNow / pPrev - 1.0)

  /// Given a matrix of daily returns (days × assets) and a weight vector,
  /// computes the portfolio’s daily return by doing a dot-product on each day.
  let portfolioDailyReturn (returnsMatrix: float[][]) (weights: float[]) : float[] =
    returnsMatrix
    |> Array.map (fun dayReturns ->
        Array.map2 (*) dayReturns weights
        |> Array.sum)

  /// Computes the annualized return given an array of daily returns.
  /// Formula: average(dailyReturns) * 252.0
  let annualizedReturn (daily: float[]) : float =
    daily
    |> Array.average
    |> fun avg -> avg * 252.0

  /// Computes the annualized volatility (standard deviation) given an array of daily returns.
  /// Formula: stddev(dailyReturns) * sqrt(252.0)
  let annualizedVolatility (daily: float[]) : float =
    let mean = Array.average daily
    let variance =
      daily
      |> Array.map (fun r -> (r - mean) ** 2.0)
      |> Array.average
    let stdDev = sqrt variance
    stdDev * sqrt 252.0

  /// (annualReturn - rf) / annualVolatility
  let sharpeRatio (annualRet: float) (annualVol: float): float =
    annualRet / annualVol

  /// Generates all k-combinations of the input list.
  /// e.g. combinations 2 [1;2;3] = [[1;2]; [1;3]; [2;3]]
  let combinations (k: int) (items: 'a list) : 'a list list =
    let rec comb acc n = function
      | _ when n = 0 -> [ List.rev acc ]
      | []           -> []
      | x::xs        ->
        comb (x::acc) (n-1) xs
        @ comb acc n xs
    comb [] k items

  /// Generates a random weight vector of length n that sums to 1.0,
  /// rejecting any draw where a component > maxPct.
  let generateWeights (n: int) (maxPct: float) (rng: Random) : float[] =
    let rec loop () =
      let raw = Array.init n (fun _ -> -log(rng.NextDouble()))
      let total = Array.sum raw
      let w = raw |> Array.map (fun x -> x / total)
      if Array.exists (fun wi -> wi > maxPct) w then loop()
      else w
    loop ()
