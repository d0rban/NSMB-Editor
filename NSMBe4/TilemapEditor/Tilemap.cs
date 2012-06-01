﻿using System;
using System.Collections.Generic;
using System.Text;
using NSMBe4.DSFileSystem;
using System.Drawing;

namespace NSMBe4
{
    public class Tilemap
    {
        public Tile[,] tiles;
        public int width, height;
        protected File f;

        public Palette[] palettes;
        public Image2D tileset;
        public int tileCount;

        public Bitmap[] buffers;

        public int tileOffset;
        public int paletteOffset;

        public Tilemap(File f, int tileWidth, Image2D i, Palette[] pals, int tileOffset, int paletteOffset)
        {
            this.f = f;
            this.width = tileWidth;
            this.tileOffset = tileOffset;
            this.paletteOffset = paletteOffset;

            this.tileset = i;
            this.palettes = pals;
            this.tileCount = tileset.getWidth() * tileset.getHeight() / 64;

            load();
        }

        public void beginEdit()
        {
            f.beginEdit(this);
        }

        public void endEdit()
        {
            f.endEdit(this);
        }

        protected virtual void load()
        {
            height = (f.fileSize / 2 + width-1) /width;
            tiles = new Tile[width, height];

            ByteArrayInputStream b = new ByteArrayInputStream(f.getContents());

            for (int i = 0; i < f.fileSize / 2; i++)
            {
                int x = i % width;
                int y = i / width;
                tiles[x,y] = shortToTile(b.readUShort());
            }
        }

        public virtual void save()
        {
            ByteArrayOutputStream os = new ByteArrayOutputStream();

            for (int i = 0; i < f.fileSize / 2; i++)
            {
                int x = i % width;
                int y = i / width;
                os.writeUShort(tileToShort(tiles[x, y]));
            }
            f.replace(os.getArray(), this);
        }

        public Tile getTileAtPos(int x, int y)
        {
            return tiles[x,y];
        }

        public void updateBuffers()
        {
            buffers = new Bitmap[palettes.Length];

            for (int i = 0; i < palettes.Length; i++)
                buffers[i] = tileset.render(palettes[i]);
        }

        public Bitmap buffer;
        Graphics bufferGx;

        Bitmap tile;
        Graphics tileg;

        public Bitmap render()
        {
            if(buffers == null)
                updateBuffers();

            if (buffer == null)
            {
                buffer = new Bitmap(width * 8, height * 8, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
                bufferGx = Graphics.FromImage(buffer);
                bufferGx.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;

                tile = new Bitmap(8, 8, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
                tileg = Graphics.FromImage(tile);
                tileg.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
            }

            reRenderAll();

            return buffer;
        }

        public Bitmap reRenderAll()
        {
            updateBuffers();
            return reRender(0, 0, width, height);
        }

        public Bitmap reRender(int xMin, int yMin, int width, int height)
        {
            for (int x = xMin; x < xMin+width; x++)
                for (int y = yMin; y < yMin+height; y++)
                {
                    if (x >= this.width) continue;
                    if (y >= this.height) continue;
                    Tile t = tiles[x, y];

                    if (t.tileNum < 0 || t.tileNum >= tileCount)
                    {
                        bufferGx.FillRectangle(Brushes.Transparent, x * 8, y * 8, 8, 8);
                        continue;
                    }
                    if (t.palNum >= palettes.Length) continue;
                    if (t.palNum < 0) continue;
                    if (t.hflip == false && t.vflip == false)
                        bufferGx.DrawImage(buffers[t.palNum], x * 8, y * 8, Image2D.getTileRectangle(buffers[t.palNum], 8, t.tileNum), GraphicsUnit.Pixel);
                    else
                    {
                        tileg.DrawImage(buffers[t.palNum], 0, 0, Image2D.getTileRectangle(buffers[t.palNum], 8, t.tileNum), GraphicsUnit.Pixel);
                        if (t.hflip == true && t.vflip == false)
                            tile.RotateFlip(RotateFlipType.RotateNoneFlipX);
                        else if (t.hflip == false && t.vflip == true)
                            tile.RotateFlip(RotateFlipType.RotateNoneFlipY);
                        else if (t.hflip == true && t.vflip == true)
                            tile.RotateFlip(RotateFlipType.RotateNoneFlipXY);
                        bufferGx.DrawImage(tile, x * 8, y * 8);
                    }
                }
            return buffer;
        }

        public ushort tileToShort(Tile t)
        {
            ushort res = 0;

            if(t.tileNum != -1)
                res |= (ushort)((t.tileNum + tileOffset) & 0x3FF);
            res |= (ushort)((t.hflip ? 1 : 0) << 10);
            res |= (ushort)((t.vflip ? 1 : 0) << 11);
            res |= (ushort)(((t.palNum + paletteOffset) & 0x00F) << 12);

            return res;
        }

        public Tile shortToTile(ushort u)
        {
            Tile res = new Tile();

            res.tileNum = (u & 0x3FF) - tileOffset;
            if (res.tileNum < 0)
                res.tileNum = -1;

            res.hflip = ((u >> 10) & 1) == 1;
            res.vflip = ((u >> 11) & 1) == 1;
            res.palNum = ((u >> 12) & 0xF) - paletteOffset;

            return res;
        }

        public struct Tile
        {
            public int tileNum;
            public int palNum;
            public bool hflip;
            public bool vflip;
        }
    }
}
