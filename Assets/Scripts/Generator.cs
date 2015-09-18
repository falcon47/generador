//************************************************************************************
/* Desarrollado por Airam González Hernández para Unity 4.5
 * 04/07/2015
 * Generador de terrenos aleatorios con Unity 3D.
 * TFG Ull.  
 * falconair@gmail.com 
 * */





using UnityEngine;
using System.Collections;
using System.Linq;

public class Generator : MonoBehaviour {

    //Terreno y ruido
    public int TipoDeTerreno = 0; // Semilla para el suelo. Variar de 1 en 1 para diferentes mapas.
    public int TipoDeMontaña = 1; // Semilla para las montañas. Variar de 1 en 1 para diferentes mapas.
    public float RuidoDeTerreno = 800.0f; // Frencuencia para el ruido de las planices. A menor frecuencia mayor rugosidad.
    public float RuidoDeMontaña = 1200.0f; // Frecuencia para el ruido de las montañas. Valores muy bajos/altos dan absurdos. En terminos medios una frecuencia relativamente baja da montañas mas suaves y una alta da cordilleras. ( rangos 400-1500 )
    public int TamañoDelHeightmap = 513; // Altura del heightmap. Por defecto unity la prefiere a 513. Da absurdos con extremas.
    public int TamañoDelTerreno = 2048; // Tamaño de cada Tile de terreno
    public int AlturaMaxima = 512; // Alturas máximas del terreno. Menor altura, montañas menos pronunciadas.
    public int CuadrantesEnX = 2; //Tiles en el eje X
    public int CuadrantesEnZ = 2; //Tiles en el eje Z
    public float DibujadoLowRes = 1000.0f; // Distancia de dibujado del low-res. A menor distancia mejor rendimiento.
    //Texturas
    public Texture2D Textura0, Textura1, Textura2, Textura3; // una variable por cada textura que queramos añadir
    public float AlphaCorte1 = 0.5f; // Altura máxima a la que se aplicarán las primeras dos texturas
    public float AlphaCorte2 = 0.75f; // Altura máxima a la que se aplicará la textura numero 2 y 3. Desde este corte hasta el maximo se aplicarán la 3 y la 4
    //Árboles
    public int TipoDeArboleda = 2;//Semilla de ruido para la generación de árboles.
    public GameObject Arbol1, Arbol2, Arbol3; // Nuestros árboles.
    public float RuidoDeArboles = 400.0f;//Al igual que con el terreno ( montañas y suelo ) frecuencia para el ruido de los árboles.
    public int EspaciadoMinimo = 32; //Espaciado mínimo entre árboles.
    public float AlturaDeArboles = 0.4f; //Porcentaje de la altura máxima a la que se pintaran arboles.



    //Privadas
    PerlinNoise ruidosuelo, ruidomontaña, ruidoarbol, ruidodetalle; // Objetos de la clase perlin para tener semillas y ruido para suelo y montaña separadas. Tambien creamos un objeto para generar árboles con perlin noise.
    Vector2 map_offset; // Offset para superposición de los tiles
    Terrain[,] mi_terrain; // Terreno de cada tile. 
    SplatPrototype[] splatArray; // Vector de texturas del mapa.
    float pesosplat0 = 10.0f; // Peso de la primera textura
    float pesosplat1 = 2.0f;
    float pesosplat2 = 2.0f;
    float pesosplat3 = 5.0f;
    int tamAlpha = 1024;
    float dist_arboles = 2000.0f; //Distancia máxima a la que se dibujan árboles. A partir de aquí ya no se dibujarán más.
    float dist_bill_arboles = 400.0f; //Distancia máxima en la que las mallas del árbol se convertiran en un billboard de árbol.
    float trans_arboles = 20.0f; //Según convertimos los árboles a billboard, hay un transform asociado para hacer coincidir las mallas. Mayor número en este parámetro hará la transición más limpia, pero afecta mucho al rendimiento.
    int max_arboles = 400; //Número máximo de árboles dibujados por área.
    TreePrototype[] treeArray; // Vector de árboles para el mapa.

    //DETALLES
    public DetailRenderMode EstiloDeDetallado;
    public int TipoDeDetallado = 3;
    public float RuidoDeDetalles = 100.0f;
    public Texture2D detalle1, detalle2, detalle3;
    public float FuerzaDeViento = 0.4f;
    public float CantidadDeViento = 0.2f;
    public float VelocidadDeViento = 0.4f;
    int dist_detalles = 400; //Distancia máxima a partir de la cual ya no se dibujan detalles.
    float densidad_detalles = 4.0f; //Creates more dense details within patch
    int res_detalles = 32; 
    Color rastro_animacion = Color.white;
    Color color_hierba = Color.white;
    Color color_hierba2 = Color.white;
    int tamDetail = 512; //Debe ser multiplo de dos
    DetailPrototype[] detailArray;
    //**************************************************

    void Start()
    {
        //Constructores de ruidos
        ruidosuelo = new PerlinNoise(TipoDeTerreno);
        ruidomontaña = new PerlinNoise(TipoDeMontaña);
        ruidoarbol = new PerlinNoise(TipoDeArboleda);
        ruidodetalle = new PerlinNoise(TipoDeDetallado);
        //***********************************************
        mi_terrain = new Terrain[CuadrantesEnX, CuadrantesEnZ];
        float[,] alturas = new float[TamañoDelHeightmap, TamañoDelHeightmap];
        map_offset = new Vector2(-TamañoDelTerreno * CuadrantesEnX * 0.5f, -TamañoDelTerreno * CuadrantesEnZ * 0.5f);//Centrado del terreno
        CreateProtoTypes();
        float max_altura = 0;
        for (int x = 0; x < CuadrantesEnX; x++)
        {
            for (int z = 0; z < CuadrantesEnZ; z++)
            {
                editTerrain(alturas, x, z);

                TerrainData terrainData = new TerrainData();

                terrainData.heightmapResolution = TamañoDelHeightmap;
                terrainData.SetHeights(0, 0, alturas);
                terrainData.size = new Vector3(TamañoDelTerreno, AlturaMaxima, TamañoDelTerreno);
                terrainData.splatPrototypes = splatArray;
                terrainData.treePrototypes = treeArray;
                terrainData.detailPrototypes = detailArray;
                max_altura = maxHeight(terrainData, max_altura);
                texturizeTerrain(terrainData, max_altura);


                mi_terrain[x, z] = Terrain.CreateTerrainGameObject(terrainData).GetComponent<Terrain>();
                mi_terrain[x, z].transform.position = new Vector3(TamañoDelTerreno * x + map_offset.x, 0, TamañoDelTerreno * z + map_offset.y);
                mi_terrain[x, z].basemapDistance = DibujadoLowRes;

                TreeGenerator(mi_terrain[x, z], x, z);
                DetailGenerator(mi_terrain[x, z], x, z);
            }
        }
        //Eliminar bordes extraños
        for (int x = 0; x < CuadrantesEnX; x++)
        {
            for (int z = 0; z < CuadrantesEnZ; z++)
            {
                Terrain right = null;
                Terrain left = null;
                Terrain bottom = null;
                Terrain top = null;

                if (x > 0) left = mi_terrain[(x - 1), z];
                if (x < CuadrantesEnX - 1) right = mi_terrain[(x + 1), z];

                if (z > 0) bottom = mi_terrain[x, (z - 1)];
                if (z < CuadrantesEnZ - 1) top = mi_terrain[x, (z + 1)];

                mi_terrain[x, z].SetNeighbors(left, top, right, bottom);

            }
        }//******************************************************
    }

    void Update()
    {

    }

    void editTerrain(float[,] alturas, int tileX, int tileZ)
    {
        float ratio = (float)TamañoDelTerreno / (float)TamañoDelHeightmap;
        for (int x = 0; x < TamañoDelHeightmap; x++)
        {
            for (int z = 0; z < TamañoDelHeightmap; z++)
            {
                float PosX = (x + tileX * (TamañoDelHeightmap - 1)) * ratio;
                float PosZ = (z + tileZ * (TamañoDelHeightmap - 1)) * ratio;

                float montaña = Mathf.Max(0.0f, ruidomontaña.FractalNoise2D(PosX, PosZ, 6, RuidoDeMontaña, 0.8f));

                float llanura = ruidosuelo.FractalNoise2D(PosX, PosZ, 4, RuidoDeTerreno, 0.1f) + 0.1f;
                alturas[z, x] = montaña + llanura;
            }
        }
    }
    float maxHeight(TerrainData terrainData, float max_altura)
    {
        for (int x = 0; x < tamAlpha; x++)
        {
            for (int z = 0; z < tamAlpha; z++)
            {
                float normX = x * 1.0f / (tamAlpha - 1);
                float normZ = z * 1.0f / (tamAlpha - 1);
                float height = terrainData.GetHeight(Mathf.RoundToInt(normX * terrainData.heightmapHeight), Mathf.RoundToInt(normZ * terrainData.heightmapWidth));
                if (height > max_altura)
                    max_altura = height;
            }
        }
        return (max_altura);
    }
    void texturizeTerrain(TerrainData terrainData, float max_altura)
    {
        float[, ,] map = new float[tamAlpha, tamAlpha, terrainData.alphamapLayers];
        Random.seed = 0;
        
        
        for (int x = 0; x < tamAlpha; x++)
        {
            for (int z = 0; z < tamAlpha; z++)
            {
                //** texturizado
                
                float normX = x * 1.0f / (tamAlpha - 1);
                float normZ = z * 1.0f / (tamAlpha - 1);

                float height = terrainData.GetHeight(Mathf.RoundToInt(normX * terrainData.heightmapHeight), Mathf.RoundToInt(normZ * terrainData.heightmapWidth));


                float[] splatWeights = new float[terrainData.alphamapLayers];
                float peso = 0;


                if (height <= max_altura * AlphaCorte1)
                {                    
                    peso = height / (max_altura * AlphaCorte1);
                    splatWeights[0] = 1 - peso;                    
                    splatWeights[1] = peso;
                }
                if ((height > max_altura * AlphaCorte1) && (height <= max_altura * AlphaCorte2))
                {
                    float altura2 = height - (max_altura * AlphaCorte1);
                    float maximo2 = (max_altura * AlphaCorte2) - (max_altura * AlphaCorte1);
                    peso = altura2 / maximo2;
                    splatWeights[1] = 1 - peso;
                    splatWeights[2] = peso;
                }
                if ((height > max_altura * AlphaCorte2) && (height <= max_altura))
                {
                    float altura2 = height - (max_altura * AlphaCorte2);
                    float maximo2 = max_altura - (max_altura * AlphaCorte2);
                    peso = altura2 / maximo2;
                    splatWeights[2] = 1 - peso;
                    splatWeights[3] = peso;
                }
                    
                
                float suma = splatWeights.Sum();

                //Recorremos las texturas y las pintamos.
                for (int i = 0; i < terrainData.alphamapLayers; i++)
                {
                    splatWeights[i] /= suma;
                    map[z, x, i] = splatWeights[i];
                }

            }
        }

        terrainData.alphamapResolution = tamAlpha;
        terrainData.SetAlphamaps(0, 0, map);
    }
    void TreeGenerator(Terrain terrain, int tileX, int tileZ)
    {
        Random.seed = 0;

        for (int x = 0; x < TamañoDelTerreno; x += EspaciadoMinimo)
        {
            for (int z = 0; z < TamañoDelTerreno; z += EspaciadoMinimo)
            {

                float unit = 1.0f / (TamañoDelTerreno - 1);

                float offsetX = Random.value * unit * EspaciadoMinimo;
                float offsetZ = Random.value * unit * EspaciadoMinimo;

                float normX = x * unit + offsetX;
                float normZ = z * unit + offsetZ;

                float inclinacion = terrain.terrainData.GetSteepness(normX, normZ);

       
                float angulo = inclinacion / 90.0f;

                if (angulo < 0.5f)//Que no estén a más de 50º 
                {
                    float PosX = x + tileX * (TamañoDelTerreno - 1);
                    float PosZ = z + tileZ * (TamañoDelTerreno - 1);

                    float ruido = ruidoarbol.FractalNoise2D(PosX, PosZ, 3, RuidoDeArboles, 1.0f);
                    float altura = terrain.terrainData.GetInterpolatedHeight(normX, normZ);

                    if (ruido > 0.0f && altura < AlturaMaxima * AlturaDeArboles)
                    {

                        TreeInstance arbolitos = new TreeInstance();
                        arbolitos.position = new Vector3(normX, altura, normZ);
                        arbolitos.prototypeIndex = Random.Range(0, 3);
                        arbolitos.widthScale = 1;
                        arbolitos.heightScale = 1;
                        arbolitos.color = Color.white;
                        arbolitos.lightmapColor = Color.white;

                        terrain.AddTreeInstance(arbolitos);
                    }
                }

            }
        }

        terrain.treeDistance = dist_arboles;
        terrain.treeBillboardDistance = dist_bill_arboles;
        terrain.treeCrossFadeLength = trans_arboles;
        terrain.treeMaximumFullLODCount = max_arboles;

    }
    void DetailGenerator(Terrain terrain, int tileX, int tileZ)
    {

        int[,] detail0 = new int[tamDetail, tamDetail];
        int[,] detail1 = new int[tamDetail, tamDetail];
        int[,] detail2 = new int[tamDetail, tamDetail];

        float ratio = (float)TamañoDelTerreno / (float)tamDetail;

        Random.seed = 0;

        for (int x = 0; x < tamDetail; x++)
        {
            for (int z = 0; z < tamDetail; z++)
            {
                detail0[z, x] = 0;
                detail1[z, x] = 0;
                detail2[z, x] = 0;

                float unit = 1.0f / (tamDetail - 1);
                float normX = x * unit;
                float normZ = z * unit;

                float inclinacion = terrain.terrainData.GetSteepness(normX, normZ);

                float angulo = inclinacion / 90.0f;

                if (angulo < 0.5f)
                {
                    float worldPosX = (x + tileX * (tamDetail - 1)) * ratio;
                    float worldPosZ = (z + tileZ * (tamDetail - 1)) * ratio;

                    float ruido = ruidodetalle.FractalNoise2D(worldPosX, worldPosZ, 3, RuidoDeDetalles, 1.0f);

                    if (ruido > 0.0f)
                    {
                        float rng = Random.value;
                        if (rng < 0.33f)
                            detail0[z, x] = 1;
                        else if (rng < 0.66f)
                            detail1[z, x] = 1;
                        else
                            detail2[z, x] = 1;
                    }
                }

            }
        }

        terrain.terrainData.wavingGrassStrength = FuerzaDeViento;
        terrain.terrainData.wavingGrassAmount = CantidadDeViento;
        terrain.terrainData.wavingGrassSpeed = VelocidadDeViento;
        terrain.terrainData.wavingGrassTint = rastro_animacion;
        terrain.detailObjectDensity = densidad_detalles;
        terrain.detailObjectDistance = dist_detalles;
        terrain.terrainData.SetDetailResolution(tamDetail, res_detalles);

        terrain.terrainData.SetDetailLayer(0, 0, 0, detail0);
        terrain.terrainData.SetDetailLayer(0, 0, 1, detail1);
        terrain.terrainData.SetDetailLayer(0, 0, 2, detail2);

    }
    void CreateProtoTypes()
    {

        //Creador de los prototypes, tanto para los splat ( texturas ) como los árboles y los adornos extra
        
        //ZONA TEXTURAS
        splatArray = new SplatPrototype[4]; // Array de texturas
        //Primera textura, replicar incrementando por cada textura
        splatArray[0] = new SplatPrototype();
        splatArray[0].texture = Textura0;
        splatArray[0].tileSize = new Vector2(pesosplat0, pesosplat0);
        //Fin primera textura
        splatArray[1] = new SplatPrototype();
        splatArray[1].texture = Textura1;
        splatArray[1].tileSize = new Vector2(pesosplat1, pesosplat1);

        splatArray[2] = new SplatPrototype();
        splatArray[2].texture = Textura2;
        splatArray[2].tileSize = new Vector2(pesosplat2, pesosplat2);

        splatArray[3] = new SplatPrototype();
        splatArray[3].texture = Textura3;
        splatArray[3].tileSize = new Vector2(pesosplat3, pesosplat3);
        //******************************

        //ZONA ÁRBOLES
        treeArray = new TreePrototype[3];

        treeArray[0] = new TreePrototype();
        treeArray[0].prefab = Arbol1;

        treeArray[1] = new TreePrototype();
        treeArray[1].prefab = Arbol2;

        treeArray[2] = new TreePrototype();
        treeArray[2].prefab = Arbol3;
        //*************************************************

        //ZONA HIERBA
        detailArray = new DetailPrototype[3];

        detailArray[0] = new DetailPrototype();
        detailArray[0].prototypeTexture = detalle1;
        detailArray[0].renderMode = EstiloDeDetallado;
        detailArray[0].healthyColor = color_hierba;
        detailArray[0].dryColor = color_hierba2;

        detailArray[1] = new DetailPrototype();
        detailArray[1].prototypeTexture = detalle2;
        detailArray[1].renderMode = EstiloDeDetallado;
        detailArray[1].healthyColor = color_hierba;
        detailArray[1].dryColor = color_hierba2;

        detailArray[2] = new DetailPrototype();
        detailArray[2].prototypeTexture = detalle3;
        detailArray[2].renderMode = EstiloDeDetallado;
        detailArray[2].healthyColor = color_hierba;
        detailArray[2].dryColor = color_hierba2;
        //**************************************************************
    }

}
