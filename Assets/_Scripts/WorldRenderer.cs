using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class WorldRenderer : MonoBehaviour
{
    public Tilemap groundTilemap, perlind2dTilemap, backgroundTilemap;

    public void SetPerlin2d(int x, int y, TileBase tile)
    {
        perlind2dTilemap.SetTile(perlind2dTilemap.WorldToCell(new Vector3Int(x, y, 0)), tile);
    }
    public void SetGroundTile(int x, int y, TileBase tile)
    {
        groundTilemap.SetTile(groundTilemap.WorldToCell(new Vector3Int(x, y, 0)), tile);
    }

    public void SetBackgroundTile(int x, int y, TileBase tile)
    {
        backgroundTilemap.SetTile(backgroundTilemap.WorldToCell(new Vector3Int(x, y, 0)), tile);
    }


    public void ClearGroundTilemap()
    {
        groundTilemap.ClearAllTiles();
        backgroundTilemap.ClearAllTiles();
    }

    public void ClearPerlind2DTilemap()
    {
        perlind2dTilemap.ClearAllTiles();
    }
}
