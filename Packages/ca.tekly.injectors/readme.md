# Injectors
A dependency injection library. Provides very basic functionality.

## Concepts

- InjectorContainer
	- Contains the instances of objects that will be injected into other objects
	- You can register instances or use factory functions
	- InjectorContainers can be constructed with a parent InjectorContainer
		- If an InjectorContainer doesn't contain a dependency it will then look in its parent
- Injector
	- Given an InjectorContainer and a target object this will inject the dependencies into the target
	- Only fields marked with `[Inject]` and methods marked with `[Inject]` will be injected into
- HierarchyInjector
	- A MonoBehaviour that collects child MonoBehaviours that have `[Inject]` on any fields or methods
	- This is only done in the editor at edit time. It will not find MonoBehaviours that are dynamically added
	- I generally put a `HierarchyInjector` at the root of prefabs and use that to inject into the prefabs components when I instantiate it


## Example

```csharp
public interface IGameService 
{
	Camera { get; }
}

public class GameService : IGameService 
{ 
	public Camera { get; private set;}

	public GameService(Camer camera)
	{
		Camera = camera;
	}
}
    
public class Enemy
{
    [Inject] public IGameService GameService;
}

public class EnemyPawn : MonoBehaviour
{
	[SerializeField] private HierarchyInjector m_hierarchyInjector;
	[Inject] private Enemy m_enemy;

	private void Awake()
	{
		// The object won't have been injected into yet
	}

	private void Start()
	{
		// The object likely won't have been injected into yet
	}

	[Inject]
	private void Initialize(InjectorContainer container)
	{
		// Called when the EnemyPawn is injected into
		// The parameters to Initialize will also be injected into.
		// InjectorContainer doing the injecting will always be available to be injected

		// This will inject into all children MonoBehaviours that have any fields or methods with [Inject] on them
		m_hierarchyInjector.Inject(container);

		// Do some stuff with m_enemy
	}
}

public class App : MonoBehaviour
{
	[SerializeField] private Camera m_camera;
	[SerializeField] private EnemyPawn m_enemyPawnTemplate;
	
	private EnemyPawn m_enemyPawnA;
    	private EnemyPawn m_enemyPawnB;

	public void Awake()
	{
	        var container = new InjectorContainer();
	        
	        // Registers GameService as the subgke implementation for IGameService. 
	        // It will be constructed on demand.
	        container.Singleton<GameService, IGameService>(); 
	        
	        // Registers Enemy to the container. 
	        // A new Enemy will be created whenever an Enemy needs to be injected into something.
	        container.Factory<Enemy>(); 

		// Registers m_camera as the instance that will be injected when a Camera is needed
		container.Register(m_camera);

		m_enemyPawnA = Instantiate(m_enemyPawnTemplate);
		container.Inject(m_enemyPawnA);

		m_enemyPawnB = Instantiate(m_enemyPawnTemplate);
		container.Inject(m_enemyPawnB);

		// m_enemyPawnA and m_enemyPawnB will have different Enemy instances. But those Enemy instances will have the same IGameService
	}    
}
```
