namespace CollageMaker.BentoLayout;

internal static class BentoLayoutEngine
{
    public static BentoLayoutResult Calculate(IReadOnlyList<BentoImageItem> items, BentoLayoutConfig config)
    {
        if (items.Count == 0)
            return new BentoLayoutResult([], 0);

        double targetAspectRatio = (double)config.CanvasWidth / config.CanvasHeight;
        var rnd = new Random();

        var activeIndices = Enumerable.Range(0, items.Count).ToList();
        int currentDropCount = 0;

        BentoTreeNode? bestTree = null;
        double bestScore = double.MaxValue;
        double bestAspectError = double.MaxValue;

        while (currentDropCount <= config.MaxDropCount && activeIndices.Count > 0)
        {
            for (int i = 0; i < config.Iterations; i++)
            {
                var shuffled = activeIndices.OrderBy(_ => rnd.Next()).ToList();
                var (root, maxImbalance) = BuildBalancedTree(shuffled, items, rnd, config.SizeBalanceWeight);

                double aspectError = Math.Abs(root.AspectRatio - targetAspectRatio);
                double score = ComputeScore(aspectError, maxImbalance, targetAspectRatio, config.SizeBalanceWeight);

                if (score < bestScore)
                {
                    bestScore = score;
                    bestAspectError = aspectError;
                    bestTree = root;
                }
            }

            if (bestAspectError <= targetAspectRatio * config.FitTolerance)
                break;

            if (currentDropCount >= config.MaxDropCount)
                break;

            double avgRatio = activeIndices.Average(idx => items[idx].AspectRatio);
            int worstIdx = activeIndices
                .OrderByDescending(idx => Math.Abs(items[idx].AspectRatio - avgRatio))
                .First();
            activeIndices.Remove(worstIdx);
            currentDropCount++;
            bestScore = double.MaxValue;
            bestAspectError = double.MaxValue;
        }

        if (bestTree == null)
            return new BentoLayoutResult([], items.Count);

        CalculateBounds(bestTree, new BentoLayoutBounds(0, 0, config.CanvasWidth, config.CanvasHeight), config.Spacing);

        var leaves = CollectLeaves(bestTree);
        var placements = leaves
            .Select(leaf => new BentoPlacement(leaf.OriginalIndex, leaf.Bounds!))
            .ToList();

        return new BentoLayoutResult(placements, items.Count - placements.Count);
    }

    /// <summary>
    /// Composite score blending normalised aspect-ratio error with worst-split imbalance.
    /// sizeBalanceWeight=0 → pure aspect-ratio fit; sizeBalanceWeight=1 → pure size balance.
    /// </summary>
    private static double ComputeScore(
        double aspectError, double maxImbalance, double targetAspectRatio, double sizeBalanceWeight)
    {
        double normalizedAspectError = aspectError / targetAspectRatio; // 0 = perfect
        double normalizedImbalance = maxImbalance * 2;                  // 0 = 50/50, 1 = 100/0
        return normalizedAspectError * (1 - sizeBalanceWeight) + normalizedImbalance * sizeBalanceWeight;
    }

    /// <summary>
    /// Builds a balanced binary tree from a list of image indices.
    /// Aspect ratios are computed inline so the caller can score without a second pass.
    /// When optimalCutBias > 0, cut direction is chosen to give a more equal area split
    /// with that probability instead of randomly.
    /// Returns the root node and the worst (max) split imbalance in [0, 0.5].
    /// </summary>
    private static (BentoTreeNode Node, double MaxImbalance) BuildBalancedTree(
        List<int> indices, IReadOnlyList<BentoImageItem> items, Random rnd, double optimalCutBias)
    {
        if (indices.Count == 1)
        {
            var leaf = new BentoTreeNode { OriginalIndex = indices[0] };
            leaf.AspectRatio = items[indices[0]].AspectRatio;
            return (leaf, 0.0);
        }

        int mid = indices.Count / 2;
        var (left, leftImbalance) = BuildBalancedTree(indices.Take(mid).ToList(), items, rnd, optimalCutBias);
        var (right, rightImbalance) = BuildBalancedTree(indices.Skip(mid).ToList(), items, rnd, optimalCutBias);

        bool isVerticalCut;
        if (optimalCutBias > 0 && rnd.NextDouble() < optimalCutBias)
        {
            // Vertical split: images sit side-by-side with equal height.
            //   Left fraction of total width = leftAR / (leftAR + rightAR)
            // Horizontal split: images stack with equal width.
            //   Top fraction of total height = (1/leftAR) / (1/leftAR + 1/rightAR)
            // Choose whichever direction gives a split closer to 50/50.
            double vertFrac = left.AspectRatio / (left.AspectRatio + right.AspectRatio);
            double horizFrac = (1.0 / left.AspectRatio) / (1.0 / left.AspectRatio + 1.0 / right.AspectRatio);
            isVerticalCut = Math.Abs(vertFrac - 0.5) < Math.Abs(horizFrac - 0.5);
        }
        else
        {
            isVerticalCut = rnd.NextDouble() > 0.5;
        }

        double aspectRatio, splitFrac;
        if (isVerticalCut)
        {
            aspectRatio = left.AspectRatio + right.AspectRatio;
            splitFrac = left.AspectRatio / aspectRatio;
        }
        else
        {
            aspectRatio = 1.0 / (1.0 / left.AspectRatio + 1.0 / right.AspectRatio);
            splitFrac = (1.0 / left.AspectRatio) / (1.0 / left.AspectRatio + 1.0 / right.AspectRatio);
        }

        // Imbalance of this split: 0 = perfect 50/50, 0.5 = one side gets everything
        double thisImbalance = Math.Max(splitFrac, 1.0 - splitFrac) - 0.5;
        double maxImbalance = Math.Max(thisImbalance, Math.Max(leftImbalance, rightImbalance));

        var node = new BentoTreeNode
        {
            Left = left,
            Right = right,
            IsVerticalCut = isVerticalCut,
            AspectRatio = aspectRatio,
        };

        return (node, maxImbalance);
    }

    private static void CalculateBounds(BentoTreeNode node, BentoLayoutBounds space, float spacing)
    {
        node.Bounds = space;
        if (node.IsLeaf) return;

        if (node.IsVerticalCut)
        {
            float workingWidth = space.Width - spacing;
            float leftWidth = (float)(workingWidth * (node.Left!.AspectRatio / node.AspectRatio));
            float rightWidth = workingWidth - leftWidth;

            CalculateBounds(node.Left, new BentoLayoutBounds(space.X, space.Y, leftWidth, space.Height), spacing);
            CalculateBounds(node.Right!, new BentoLayoutBounds(space.X + leftWidth + spacing, space.Y, rightWidth, space.Height), spacing);
        }
        else
        {
            float workingHeight = space.Height - spacing;
            double leftInverse = 1.0 / node.Left!.AspectRatio;
            double totalInverse = leftInverse + 1.0 / node.Right!.AspectRatio;
            float leftHeight = (float)(workingHeight * (leftInverse / totalInverse));
            float rightHeight = workingHeight - leftHeight;

            CalculateBounds(node.Left, new BentoLayoutBounds(space.X, space.Y, space.Width, leftHeight), spacing);
            CalculateBounds(node.Right, new BentoLayoutBounds(space.X, space.Y + leftHeight + spacing, space.Width, rightHeight), spacing);
        }
    }

    private static List<BentoTreeNode> CollectLeaves(BentoTreeNode node)
    {
        if (node.IsLeaf)
            return [node];

        var leaves = new List<BentoTreeNode>();
        if (node.Left != null) leaves.AddRange(CollectLeaves(node.Left));
        if (node.Right != null) leaves.AddRange(CollectLeaves(node.Right));
        return leaves;
    }
}
