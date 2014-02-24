﻿using System;
using System.Text;
using GSF;
using GSF.IO.Unmanaged;
using GSF.SortedTreeStore.Tree.TreeNodes;

namespace GSF.SortedTreeStore.Tree
{
    public abstract class TreeNodeRandomizerBase<TKey, TValue>
        where TKey : class, new()
        where TValue : class, new()
    {
        public abstract void Reset(int maxCount);
        public abstract void Next();
        public abstract void GetRandom(int index, TKey key, TValue value);
        public abstract void GetInSequence(int index, TKey key, TValue value);
    }

    public class LeafNodeTest
    {
        private const int Max = 1000000;

        public static void TestNode<TKey, TValue>(SortedTreeNodeBase<TKey, TValue> node, TreeNodeRandomizerBase<TKey, TValue> randomizer, int count)
            where TKey : SortedTreeTypeBase<TKey>, new()
            where TValue : SortedTreeTypeBase<TValue>, new()
        {
            int Max = count;
            uint rootKey = 0;
            byte rootLevel = 0;

            uint nextKeyIndex = 2;
            bool hasChanged = false;
            Func<uint> getNextKey = () =>
            {
                nextKeyIndex++;
                return nextKeyIndex - 1;
            };

            StringBuilder sb = new StringBuilder();

            using (BinaryStream bs = new BinaryStream())
            {
                const int pageSize = 512;
                SparseIndex<TKey> sparse = new SparseIndex<TKey>(SortedTree.FixedSizeNode);
                sparse.Initialize(bs, pageSize, getNextKey, 0, 1);
                node.Initialize(bs, pageSize, getNextKey, sparse);
                node.CreateEmptyNode(1);

                TKey key = new TKey();
                TKey key2 = new TKey();
                TValue value = new TValue();
                TValue value2 = new TValue();

                randomizer.Reset(Max);
                for (int x = 0; x < Max; x++)
                {
                    randomizer.Next();
                    //Add the next point
                    randomizer.GetRandom(x, key, value);

                    //node.WriteNodeData(sb);

                    if (!node.TryInsert(key, value))
                        throw new Exception();

                    //node.WriteNodeData(sb);
                    //File.WriteAllText("c:\\temp\\temp.log", sb.ToString());


                    //Check if all points exist
                    for (int y = 0; y <= x; y++)
                    {
                        randomizer.GetRandom(y, key, value);
                        if (!node.TryGet(key, value2))
                            throw new Exception();
                        if (!value.IsEqualTo(value2))
                            throw new Exception();
                    }

                    //Check if scanner works.
                    SortedTreeScannerBase<TKey, TValue> scanner = node.CreateTreeScanner();
                    scanner.SeekToStart();
                    for (int y = 0; y <= x; y++)
                    {
                        randomizer.GetInSequence(y, key, value);
                        if (!scanner.Read())
                            throw new Exception();
                        if (!key.IsEqualTo(scanner.CurrentKey))
                            throw new Exception();
                        if (!value.IsEqualTo(scanner.CurrentValue))
                            throw new Exception();
                    }
                    if (scanner.Read())
                        throw new Exception();
                }

                node = node;
            }
        }


        internal static void TestSpeed<TKey, TValue>(TreeNodeInitializer<TKey, TValue> nodeInitializer, TreeNodeRandomizerBase<TKey, TValue> randomizer, int count, int pageSize)
            where TKey : SortedTreeTypeBase<TKey>, new()
            where TValue : SortedTreeTypeBase<TValue>, new()
        {
            int Max = count;

            uint nextKeyIndex = 2;
            Func<uint> getNextKey = () =>
            {
                nextKeyIndex++;
                return nextKeyIndex - 1;
            };


            using (BinaryStream bs = new BinaryStream())
            {
                randomizer.Reset(Max);
                for (int x = 0; x < Max; x++)
                {
                    randomizer.Next();
                }

                TKey key = new TKey();
                TValue value = new TValue();
                SortedTreeNodeBase<TKey, TValue> node = null;

                Console.WriteLine(StepTimer.Time(count, (sw) =>
                {
                    nextKeyIndex = 2;
                    node = nodeInitializer.CreateTreeNode(0);
                    SparseIndex<TKey> sparse = new SparseIndex<TKey>(SortedTree.FixedSizeNode);
                    sparse.Initialize(bs, pageSize, getNextKey, 0, 1);
                    node.Initialize(bs, pageSize, getNextKey, sparse);
                    node.CreateEmptyNode(1);
                    sw.Start();
                    for (int x = 0; x < Max; x++)
                    {
                        //Add the next point
                        randomizer.GetRandom(x, key, value);

                        if (!node.TryInsert(key, value))
                            throw new Exception();
                    }
                    sw.Stop();
                }));


                Console.WriteLine(StepTimer.Time(count, () =>
                {
                    for (int x = 0; x < Max; x++)
                    {
                        //Add the next point
                        randomizer.GetRandom(x, key, value);

                        if (!node.TryGet(key, value))
                            throw new Exception();
                    }
                }));


                Console.WriteLine(StepTimer.Time(count, () =>
                {
                    SortedTreeScannerBase<TKey, TValue> scanner = node.CreateTreeScanner();
                    scanner.SeekToStart();
                    while (scanner.Read())
                        ;
                }));

                node = node;
            }
        }
    }
}