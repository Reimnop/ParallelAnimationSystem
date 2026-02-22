using ParallelAnimationSystem.Util;

namespace ParallelAnimationSystem.Rendering;

public class MemoryAllocator<T>(int initialCapacity = 1024) where T : unmanaged
{
    private struct Block
    {
        public int Offset;
        public int Size;
        public bool IsFree;
    }
    
    public IReadOnlyBuffer<T> Buffer => buffer;

    public int Allocated { get; private set; }

    private readonly Buffer<T> buffer = new(initialCapacity);
    private readonly List<Block> blocks = [new()
    {
        Offset = 0,
        Size = initialCapacity,
        IsFree = true
    }];

    public int Alloc(int size)
    {
        var block = GetBlock(size);
        Allocated += block.Size;
        return block.Offset;
    }

    public void Free(int offset)
    {
        // find block by offset
        var blockIndex = blocks.BinarySearchKey(offset, x => x.Offset, Comparer<int>.Default);
        if (blockIndex < 0 || blocks[blockIndex].IsFree)
            throw new InvalidOperationException($"Offset {offset} does not correspond to any allocated block");
        
        // mark block as free
        var freeBlock = blocks[blockIndex] with { IsFree = true };
        blocks[blockIndex] = freeBlock;
        Allocated -= freeBlock.Size;
        
        // merge with previous block if free
        if (blockIndex > 0 && blocks[blockIndex - 1].IsFree)
        {
            var prevBlock = blocks[blockIndex - 1];
            freeBlock = freeBlock with { Offset = prevBlock.Offset, Size = prevBlock.Size + freeBlock.Size };
            blocks[blockIndex - 1] = freeBlock;
            blocks.RemoveAt(blockIndex);
            blockIndex--;
        }
        
        // merge with next block if free
        if (blockIndex < blocks.Count - 1 && blocks[blockIndex + 1].IsFree)
        {
            var nextBlock = blocks[blockIndex + 1];
            freeBlock = freeBlock with { Size = freeBlock.Size + nextBlock.Size };
            blocks[blockIndex] = freeBlock;
            blocks.RemoveAt(blockIndex + 1);
        }
    }

    private Block GetBlock(int size)
    {
        // find first free block that can fit the requested size
        for (var i = 0; i < blocks.Count; i++)
        {
            var block = blocks[i];
            if (block.IsFree && block.Size >= size)
            {
                // if block is larger than needed, split it
                if (block.Size > size)
                {
                    var newBlock = new Block
                    {
                        Offset = block.Offset + size,
                        Size = block.Size - size,
                        IsFree = true
                    };
                    blocks.Insert(i + 1, newBlock);
                }
                
                // mark block as used
                block.Size = size;
                block.IsFree = false;
                blocks[i] = block;
                
                return block;
            }
        }
        
        // no suitable block found, need to expand buffer
        var lastBlock = blocks[^1];
        if (lastBlock.IsFree)
        {
            // expand last block
            lastBlock = lastBlock with
            {
                Size = size,
                IsFree = false
            };
            blocks[^1] = lastBlock;
        }
        else
        {
            lastBlock = new Block
            {
                Offset = lastBlock.Offset + lastBlock.Size,
                Size = size,
                IsFree = false
            };
            blocks.Add(lastBlock);
        }
        
        // expand buffer
        var requiredSize = lastBlock.Offset + lastBlock.Size;
        buffer.EnsureSize(requiredSize);

        return lastBlock;
    }
}