namespace Library.Func

module PortfolioSimulation =
    open System
    open Library.Func.Portfolio

    /// Result of simulating a given combination of assets
    type SimulationResult = {
        Combination : int list    // indices of assets in the combination
        BestSharpe  : float       // best Sharpe achieved
        BestWeights : float[]     // weights that yielded the best Sharpe
    }

    /// Simulates a limited number of combinations in parallel.
    /// - returnsMatrix: daily returns matrix (days × assets)
    /// - assetCount: total number of assets
    /// - comboSize: number of assets per portfolio
    /// - comboLimit: how many combinations to test
    /// - maxPct: maximum weight per asset
    let simulateSomeCombinations
        (returnsMatrix: float[][])
        (assetCount:    int)
        (comboSize:     int)
        (comboLimit:    int)
        (maxPct:        float)
        : SimulationResult list =

        // 1) Generate all combinations of indices, take up to comboLimit
        let allIndices = [0 .. assetCount - 1]
        let combos = combinations comboSize allIndices |> List.take comboLimit

        // 2) Process each combination in parallel
        combos
        |> List.toArray
        |> Array.Parallel.map (fun combo ->
            // each worker gets its own RNG (non-deterministic)
            let threadRng = Random()

            // build submatrix for this combination: days × comboSize
            let subMatrix =
                returnsMatrix
                |> Array.map (fun day ->
                    combo
                    |> List.map (fun i -> day.[i])
                    |> List.toArray)

            // 3) Simulate 1000 random weightings and compute Sharpe, dropping invalids
            let sims =
                [1 .. 1000]
                |> List.map (fun _ ->
                    let w     = generateWeights combo.Length maxPct threadRng
                    let daily = portfolioDailyReturn subMatrix w
                    let retA  = annualizedReturn daily
                    let volA  = annualizedVolatility daily
                    let sr    = sharpeRatio retA volA 
                    (sr, w))
                |> List.filter (fun (sr, _) ->
                    not (Double.IsNaN sr) && not (Double.IsInfinity sr))

            // 4) Select best finite Sharpe or fallback
            let bestSharpe, bestWeights =
                match sims with
                | []   -> Double.NegativeInfinity, Array.zeroCreate combo.Length
                | xs   -> List.maxBy fst xs

            // 5) Build result record
            {
                Combination = combo
                BestSharpe  = bestSharpe
                BestWeights = bestWeights
            })
        |> Array.toList
