using System.Collections.Generic;
using System.Linq;
using Tekly.Common.Utils;
using Tekly.DevBoard.Components;
using Tekly.DevBoard.Components.Inputs;
using UnityEngine;

namespace Tekly.DevBoard
{
    public class DevBoard : Singleton<DevBoard>
    {
	    private GameObject m_devBoard;
	    private bool m_initialized;

	    private readonly Dictionary<string, Board> m_boardPrefabs = new();
	    private readonly Dictionary<string, ButtonWidget> m_buttons = new();
	    private readonly Dictionary<string, ContainerWidget> m_containers = new();
	    private readonly Dictionary<string, PropertyWidget> m_properties = new();
	    private readonly Dictionary<string, FoldoutWidget> m_foldouts = new();
	    private readonly Dictionary<string, DividerWidget> m_dividers = new();
	    private readonly Dictionary<string, TextInputWidget> m_textInputs = new();
	    private readonly Dictionary<string, IntInputWidget> m_intInputs = new();
	    private readonly Dictionary<string, FloatInputWidget> m_floatInputs = new();
	    private readonly Dictionary<string, ToggleWidget> m_toggles = new();
	    
	    public void Initialize()
	    {
		    if (!m_initialized) {
			    m_devBoard = new GameObject("DevBoard");
			    Object.DontDestroyOnLoad(m_devBoard);
			    m_initialized = true;
		    }
	    }

	    public Board GetBoard(string name)
	    {
		    return Get(name, m_boardPrefabs);
	    }
	    
	    public ButtonWidget GetButton(string name)
	    {
		    return Get(name, m_buttons);
	    }
	    
	    public ContainerWidget GetContainer(string name)
	    {
		    return Get(name, m_containers);
	    }
	    
	    public PropertyWidget GetProperty(string name)
	    {
		    return Get(name, m_properties);
	    }
	    
	    public FoldoutWidget GetFoldout(string name)
	    {
		    return Get(name, m_foldouts);
	    }
	    
	    public DividerWidget GetDivider(string name)
	    {
		    return Get(name, m_dividers);
	    }
	    
	    public TextInputWidget GetTextInput(string name)
	    {
		    return Get(name, m_textInputs);
	    }
	    
	    public IntInputWidget GetIntInput(string name)
	    {
		    return Get(name, m_intInputs);
	    }
	    
	    public FloatInputWidget GetFloatInput(string name)
	    {
		    return Get(name, m_floatInputs);
	    }
	    
	    public ToggleWidget GetToggle(string name)
	    {
		    return Get(name, m_toggles);
	    }

	    public void AddAssets(DevBoardAssets assets)
	    {
		    Initialize();
			
		    AddAssets(assets.Boards, m_boardPrefabs);
		    AddAssets(assets.Containers, m_containers);
		    AddAssets(assets.Buttons, m_buttons);
		    AddAssets(assets.Properties, m_properties);
		    AddAssets(assets.Foldouts, m_foldouts);
		    AddAssets(assets.Dividers, m_dividers);
		    AddAssets(assets.TextInputs, m_textInputs);
		    AddAssets(assets.IntInputs, m_intInputs);
		    AddAssets(assets.FloatInputs, m_floatInputs);
		    AddAssets(assets.Toggles, m_toggles);

		    DevBoardFonts.Register(assets);
	    }
	    
	    public Board Board(string name)
	    {
		    var boardPrefab = m_boardPrefabs.Values.First();
		    var board = Object.Instantiate(boardPrefab, m_devBoard.transform);
		    board.name = name;

		    return board;
	    }

	    private static void AddAssets<T>(T[] assets, Dictionary<string, T> dictionary) where T : Widget
	    {
		    foreach (var asset in assets) {
			    dictionary.Add(asset.name, asset);
		    }
	    }
	    
	    private static T Get<T>(string name, Dictionary<string, T> dictionary)
	    {
		    if (!dictionary.TryGetValue(name, out var value)) {
			    value = dictionary.Values.First();
		    }

		    return value;
	    }
    }
}
