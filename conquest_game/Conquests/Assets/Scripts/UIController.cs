using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using TMPro;

public class UIController : MonoBehaviour
{
    public Button buttonOwner, buttonTerrain, buttonRegion, buttonModifier, buttonPrompt1, buttonEnd;
    public Slider slider;
    public Dropdown dropdown;
    public MeshRenderer highlight, highlight2, overlay, underlay, ownership;
    public TextMeshProUGUI infoBoxTMP, ownerBoxTMP, activeOwnerBoxTMP, sliderTitleTMP, sliderInfoTMP;
    public TMP_FontAsset font;
    public Transform areasInfoParent;
    public GameController gc;
    Dictionary<Area,GameObject> areasInfos;
    public Area selectedArea;
    ColorSettings colorSettings;
    MaterialPropertyBlock propertyBlock;
    Texture2D transparentTex;
    
    void Awake()
    {
        areasInfos = new Dictionary<Area, GameObject>();

        propertyBlock = new MaterialPropertyBlock();

        // New textures
        transparentTex = new Texture2D(4848, 2048);
        Color32[] pix = transparentTex.GetPixels32();
        Color32 color = new Color32(0,0,0,0);
        for (int i = 0; i < pix.Length; i++)
        {
            pix[i] = color;
        }
        transparentTex.SetPixels32(pix);
        transparentTex.Apply();
        
        Texture2D highlightTex = new Texture2D(4848, 2048);
        highlight.sharedMaterial.mainTexture = highlightTex;
        ResetHighlight();

        Texture2D highlight2Tex = new Texture2D(4848, 2048);
        highlight2.sharedMaterial.mainTexture = highlight2Tex;
        ResetHighlight(true);

        Texture2D overlayTex = new Texture2D(4848, 2048);
        overlay.sharedMaterial.mainTexture = overlayTex;
        ResetOverlay();

        Texture2D ownershipTex = new Texture2D(4848, 2048);
        ownership.sharedMaterial.mainTexture = ownershipTex;
        Graphics.CopyTexture(transparentTex, ownership.sharedMaterial.mainTexture);

        Texture2D underlayTex = new Texture2D(4848, 2048);
        underlay.sharedMaterial.mainTexture = underlayTex;
        Graphics.CopyTexture(transparentTex, underlay.sharedMaterial.mainTexture);
    }

    void Start()
    {
        colorSettings = GetComponent<ColorSettings>();
    }

    public void InitialDrawOwnership()
    {
        Texture2D tex = ownership.sharedMaterial.mainTexture as Texture2D;
        Color32[] pix = tex.GetPixels32();
        int i = 0;
        foreach (Owner owner in gc.mapInfo.owners.Values)
        {
            //Skip if "0", defined as no ownership
            if (owner.name == "No Owner")
            {
                owner.AssignColor(new Color32(0,0,0,0));
                continue;
            }
            
            List<Area> areas = owner.areas;
            Color32 color = ColorSettings.GetCycledColor(i);
            owner.AssignColor(color);
            foreach (Area area in areas)
            {
                List<int> coords = area.coords;
                pix = ColorPixels(pix, coords, color);
            }
            i = i + 1;
        }
        tex.SetPixels32(pix);
        tex.Apply();
        ownership.sharedMaterial.mainTexture = tex;
    }

    public void InitialDrawUnderlay()
    {
        //Color terrain
        Texture2D tex = underlay.sharedMaterial.mainTexture as Texture2D;
        Color32[] pix = tex.GetPixels32();
        foreach (Terrain terrain in gc.mapInfo.terrains.Values)
        {
            Color32 color;
            switch (terrain.name)
            {
                case "Sea":
                    color = colorSettings.water;
                    break;
                case "Plains":
                    color = colorSettings.plains;
                    break;
                case "Mountain":
                    color = colorSettings.mountains;
                    break;
                case "Forest":
                    color = colorSettings.forest;
                    break;
                case "Ice":
                    color = colorSettings.ice;
                    break;
                case "Desert":
                    color = colorSettings.desert;
                    break;
                default: 
                    color = colorSettings.debug;
                    break;
            }
            foreach (Area area in terrain.areas)
            {
                pix = ColorAreaPixels(area, color, pix);
            }
            
        }
        tex.SetPixels32(pix);
        tex.Apply();
        underlay.sharedMaterial.mainTexture = tex;
    }

    public void UpdateOwnership(List<Area> areas)
    {
        Debug.Log("Updating ownership");
        Texture2D tex = ownership.sharedMaterial.mainTexture as Texture2D;
        Color32[] pix = tex.GetPixels32();
        foreach (Area area in areas)
        {
            List<int> coords = area.coords;
            pix = ColorPixels(pix, coords, area.owner.color);
        }
        tex.SetPixels32(pix);
        tex.Apply();
        ownership.sharedMaterial.mainTexture = tex;
    }

    public void ResetHighlight(bool secondary = false)
    {
        MeshRenderer mr = secondary ? highlight2 : highlight;
        Graphics.CopyTexture(transparentTex, mr.sharedMaterial.mainTexture);
    }

    public void ResetOverlay()
    {
        Graphics.CopyTexture(transparentTex, overlay.sharedMaterial.mainTexture);
        ownership.enabled = false;
    }

    public void SelectArea(Area area, bool setInfoBox = true)
    {
        ResetHighlight();
        HighlightArea(area);
        selectedArea = area;

        if (setInfoBox)
        {
            string areaText = string.Format(
                "Name: {0}\nRegion: {1}\nContinent: {2}\nTerrain: {3}\nModifier: {4}\nOwner: {5}\nUnits: {6}",
                area.name, area.region.name, area.continent.name, area.terrain.name, area.modifier.name, area.owner.name, string.Join("/",area.units.Select(u => u.name)));
            SetInfoBox(areaText);
        }
        
    }

    public void SetInfoBox(string text)
    {
        infoBoxTMP.text = text;
    }

    public void SetOwnerBox(string text)
    {
        ownerBoxTMP.text = text;
    }

    public void SetActiveOwnerBox(string text)
    {
        activeOwnerBoxTMP.text = text;
    }

    public void HighlightArea(Area area, bool secondary = false)
    {
        MeshRenderer mr = secondary ? highlight2 : highlight;
        Texture2D tex = mr.sharedMaterial.mainTexture as Texture2D;
        byte opacity = secondary ? (byte)100 : (byte)200;
        tex = ColorArea(area, new Color32(255,255,255,opacity), tex);
        tex.Apply();
        mr.sharedMaterial.mainTexture = tex;
    }

    public void HighlightAreas(List<Area> areas, bool secondary = false)
    {
        ResetHighlight(secondary);
        MeshRenderer mr = secondary ? highlight2 : highlight;
        Texture2D tex = mr.sharedMaterial.mainTexture as Texture2D;
        Color32[] pix = tex.GetPixels32();
        byte opacity = secondary ? (byte)100 : (byte)200;
        foreach (Area area in areas)
        {
            pix = ColorAreaPixels(area, new Color32(255,255,255,opacity), pix);
        }
        tex.SetPixels32(pix);
        tex.Apply();
        mr.sharedMaterial.mainTexture = tex;
    }

    Texture2D ColorArea(Area area, Color32 color, Texture2D tex)
    {
        List<int> coords = area.coords;
        Color32[] pix = tex.GetPixels32();
        pix = ColorPixels(pix, coords, color);
        tex.SetPixels32(pix);
        return tex;
    }

    Color32[] ColorAreaPixels(Area area, Color32 color, Color32[] pix)
    {
        List<int> coords = area.coords;
        pix = ColorPixels(pix, coords, color);
        return pix;
    }


    Color32[] ColorPixels(Color32[] pix, List<int> coords, Color32 color) {
        foreach (int i in coords)
        {
            pix[i] = color;
        }
        return pix;
    }

    public void CycleColorAreas(List<List<Area>> areasList, byte opacity = 255) 
    {   
        Texture2D tex = overlay.sharedMaterial.mainTexture as Texture2D;
        Color32[] pix = tex.GetPixels32();
        int i = 0;
        foreach (List<Area> areas in areasList)
        {
            Color color = ColorSettings.GetCycledColor(i, opacity);
            foreach (Area area in areas)
            {
                List<int> coords = area.coords;
                pix = ColorPixels(pix, coords, color);
            }
            i = i + 1;
        }
        tex.SetPixels32(pix);
        tex.Apply();
        overlay.sharedMaterial.mainTexture = tex;
    }

    public void DrawAreaInfo (Area area)
    {
        if (!areasInfos.ContainsKey(area))
        {
            GameObject info = new GameObject("Info for " + area.name);
            info.layer = LayerMask.NameToLayer("UI");
            info.transform.parent = areasInfoParent;
            TextMeshPro tmp = info.AddComponent<TextMeshPro>();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 300f;
            tmp.rectTransform.anchoredPosition = new Vector2(area.centre[0], area.centre[1]);
            tmp.rectTransform.sizeDelta = new Vector2(200f, 10f);
            areasInfos[area] = info;
            tmp.font = font;
            UpdateAreaInfo(area);
        }
    }

    public void UpdateAreaInfo (Area area)
    {
        TextMeshPro tmp = areasInfos[area].GetComponent<TextMeshPro>();
        tmp.enabled = area.units.Count != 0;
        tmp.text = area.units.Count.ToString();
    }

    public void ShowOwnership()
    {
        ResetOverlay();
        ownership.enabled = true; 
    }

    public void ShowRegions()
    {
        ResetOverlay();
        List<List<Area>> areasList = new List<List<Area>>();
        foreach (Region region in gc.mapInfo.regions.Values) {
            //Ignore is 0 (untraversable) or 1 (sea)
            if (region.name == "Untraversable" || region.name == "Water")
            {
                continue;
            }
            areasList.Add(region.areas);
        }
        CycleColorAreas(areasList);
    }

    public void ShowContinents()
    {
        ResetOverlay();
        List<List<Area>> areasList = new List<List<Area>>();
        foreach (Continent continent in gc.mapInfo.continents.Values) {
            //Ignore is 0 (untraversable) or 1 (sea)
            if (continent.name == "None")
            {
                continue;
            }
            areasList.Add(continent.areas);
        }
        CycleColorAreas(areasList);
    }

    public void ShowModifiers()
    {
        ResetOverlay();
        List<List<Area>> areasList = new List<List<Area>>();
        foreach (Modifier modifier in gc.mapInfo.modifiers.Values) {
            //Ignore is 0 (untraversable) or 1 (sea)
            if (modifier.name == "Untraversable" || modifier.name == "Water")
            {
                continue;
            }
            areasList.Add(modifier.areas);
        }
        CycleColorAreas(areasList);
    }

    public void ShowPrompt1(bool on, string msg = "Prompt 1")
    {
        buttonPrompt1.gameObject.SetActive(on);
        buttonPrompt1.interactable = true;
        buttonPrompt1.GetComponentInChildren<TextMeshProUGUI>().text = msg;
    }
    
    public void OnPrompt1Click()
    {
        if (gc.gs == GameState.DeployUnits)
        {
            gc.InitSelectMoveFrom();
        }
        else if (gc.gs == GameState.SelectMoveFrom)
        {
            gc.InitSelectMoveTo();
        }
        else if (gc.gs == GameState.SelectMoveTo)
        {
            gc.InitSelectMoveUnits();
        }
        else if (gc.gs == GameState.Pregame)
        {
            gc.ReadyUp(true);
            buttonPrompt1.interactable = false;
        }
    }

    public void OnEndTurnClick()
    {
        gc.ExecEndTurn();
    }

    public void ShowEndTurn(bool on)
    {
        buttonEnd.gameObject.SetActive(on);
    }

    public void ShowSlider(bool on, float min = -1, float max = -1, string title = "Prompt")
    {
        slider.gameObject.SetActive(on);
        //If defaults, don't change the slider numbers
        slider.minValue = min == -1 ? slider.minValue : min;
        slider.maxValue = max == -1 ? slider.maxValue : max;
        slider.value = max == -1 ? slider.value : max;
        sliderTitleTMP.text = title;
        OnSliderChange();
    }

    public void OnSliderChange()
    {
        sliderInfoTMP.text = slider.value.ToString() + "/" + slider.maxValue.ToString();
    }

    public void OnSliderConfirm()
    {
        if (gc.gs == GameState.SelectMoveUnits)
        {
            ShowSlider(false);
            gc.ExecSelectMoveTo();
        }
    }

    public void ShowDropdown(bool on, List<string> options = null)
    {
        dropdown.gameObject.SetActive(on);
        if (on)
        {
            dropdown.ClearOptions();
            dropdown.AddOptions(options);
        }
    }

    public void OnDropdownConfirm()
    {
        gc.net.CmdRequestOwnership(dropdown.options[dropdown.value].text);
    }
}
