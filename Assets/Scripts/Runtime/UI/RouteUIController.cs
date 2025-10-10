using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CreateNeptune;
using UnityEngine.Events;
using System.Linq;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;
using Unity.VisualScripting;

/// <summary>
/// Controls Route selection
/// </summary>
public class RouteUIController : MonoBehaviour
{
    [SerializeField] private Canvas canvas;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RawImage mapDislayImage;
    [SerializeField] private RenderTexture mapRenderTexture;
    [SerializeField] private Scene mapScene;
    [SerializeField] private CarouselScrollRect routeMapCardScrollRect;
    [SerializeField] private float scrollRectSnapVelocityThreshold = 10000f;
    [SerializeField] private HorizontalLayoutGroup routeMapCardLayoutGroup;
    [SerializeField] private PoolContext routeMapCardPool;
    private List<RouteMapCard> activeRouteMapCards = new();
    [SerializeField] private Button confirmButton;
    [Header("Routes View Toggle References")]
    [SerializeField] private Image routesViewToggleButtonImage;
    [SerializeField] private Image routesViewToggleButtonShadowImage;
    [SerializeField] private Sprite toggleOnSprite;
    [SerializeField] private Sprite toggleOffSprite;
    private bool routesViewToggleOn = true;

    private Route selectedRoute;
    private float cardWidth;

    private IEnumerator toggleRoutine;
    private IEnumerator scrollToggleRoutine;
    private IEnumerator scrollRoutine;

    #region Events
    public class ToggleEvent : UnityEvent<bool> { };
    public static ToggleEvent toggleEvent = new ToggleEvent();
    public class RouteSelectedEvent : UnityEvent<RouteSelectedEvent.Context>
    {
        public class Context
        {
            public Route route;
        }
    }
    public static RouteSelectedEvent routeSelectedEvent = new();
    #endregion

    private void Awake()
    {
        routeMapCardPool.Initialize();
        OnToggle(false);
    }

    private void OnEnable()
    {
        toggleEvent.AddListener(OnToggle);
        MapCameraController.mapUnselectedEvent.AddListener(OnMapUnselected);
        MapCameraController.routeLineTappedEvent.AddListener(OnRouteLineTapped);
    }

    private void OnDisable()
    {
        toggleEvent.RemoveListener(OnToggle);
        MapCameraController.mapUnselectedEvent.RemoveListener(OnMapUnselected);
        MapCameraController.routeLineTappedEvent.RemoveListener(OnRouteLineTapped);
    }

    #region Event Listeners

    private void OnToggle(bool active)
    {
        if (active)
        {
            CNExtensions.SafeStartCoroutine(this, ref toggleRoutine, CNAction.FadeObject(canvas, GameManager.Instance.DefaultUIAnimationTime, canvasGroup.alpha, 1, CNEase.EaseType.Linear, true, false, true));

            Route[] routes = RouteModel.Instance.Routes.Where(r => r.saveData.data.unlocked).ToArray();
            for (int i = 0; i < routes.Length; i++)
            {
                RouteMapCard rmc = routeMapCardPool.GetPooledObject<RouteMapCard>();
                rmc.Setup(routes[i]);
                activeRouteMapCards.Add(rmc);
            }

            cardWidth = activeRouteMapCards[0].GetComponent<LayoutElement>().preferredWidth;
            int horizontalPadding = (int)(routeMapCardScrollRect.GetComponent<RectTransform>().rect.width - cardWidth) / 2;
            routeMapCardLayoutGroup.padding.left = horizontalPadding;
            routeMapCardLayoutGroup.padding.right = horizontalPadding;

            routeMapCardScrollRect.onValueChanged.AddListener(OnScrollRectValueChanged);

            bool mapSceneLoaded = false;
            for (int j = 0; j < SceneManager.sceneCount; j++)
            {
                if (SceneManager.GetSceneAt(j).buildIndex == (int)mapScene)
                {
                    mapSceneLoaded = true;
                    break;
                }
            }

            if (!mapSceneLoaded)
            {
                SceneManager.LoadSceneAsync((int)mapScene, LoadSceneMode.Additive);
                SceneManager.sceneLoaded += OnMapSceneLoaded;
            }
            else
            {
                StartCoroutine(SetRouteOnRetoggleRoutine());
            }
        }
        else
        {
            CNExtensions.SafeStartCoroutine(this, ref toggleRoutine, ToggleOffRoutine());
        }
    }

    private IEnumerator ToggleOffRoutine()
    {
        routeMapCardScrollRect.onValueChanged.RemoveListener(OnScrollRectValueChanged);

        yield return CNAction.FadeObject(canvas, GameManager.Instance.DefaultUIAnimationTime, canvasGroup.alpha, 0, CNEase.EaseType.Linear, false, true, true);

        activeRouteMapCards.Clear();
        routeMapCardPool.ReturnAllToPool();
    }

    private IEnumerator SetRouteOnRetoggleRoutine()
    {
        yield return null;
        SelectRoute(selectedRoute);
    }

    private void OnMapSceneLoaded(UnityEngine.SceneManagement.Scene scene, LoadSceneMode loadSceneMode)
    {
        if (scene.buildIndex == (int)mapScene)
        {
            Rect mapPixelRect = RectTransformUtility.PixelAdjustRect(mapDislayImage.rectTransform, canvas);
            mapPixelRect.width = mapPixelRect.width * canvas.scaleFactor;
            mapPixelRect.height = mapPixelRect.height * canvas.scaleFactor;

            MapCameraController.changeMapRenderResolutionEvent.Invoke(new MapCameraController.ChangeMapRenderResolutionEvent.Context
            {
                width = (int)mapPixelRect.width,
                height = (int)mapPixelRect.height
            });

            MapController.showRoutesEvent.Invoke(new MapController.ShowRoutesEvent.Context
            {
                routes = RouteModel.Instance.Routes.Where(r => r.saveData.data.unlocked).ToList()
            });

            SelectRoute(RouteModel.Instance.Routes.First(r => activeRouteMapCards[0].RouteName == r.DisplayName));

            SceneManager.sceneLoaded -= OnMapSceneLoaded;
        }
    }

    private void OnMapUnselected()
    {
        SelectRoute(null);
    }

    private void OnRouteLineTapped(MapCameraController.RouteLineTappedEvent.Context context)
    {
        SelectRoute(RouteModel.Instance.Routes.First(r => r.DisplayName == context.routeName));
    }
    #endregion

    #region UI Callbacks
    public void OnRoutesViewToggle()
    {
        routesViewToggleOn = !routesViewToggleOn;
        if (routesViewToggleOn)
        {
            routesViewToggleButtonShadowImage.sprite = toggleOnSprite;
            routesViewToggleButtonImage.sprite = toggleOnSprite;
        }
        else
        {
            routesViewToggleButtonShadowImage.sprite = toggleOffSprite;
            routesViewToggleButtonImage.sprite = toggleOffSprite;
        }

        MapController.toggleRoutesEvent.Invoke();
    }

    public void OnConfirmButton()
    {
        OnToggle(false);

        selectedRoute.saveData.data.numTimesRun++;
        RunController.startRunEvent.Invoke(new RunController.StartRunEvent.Context
        {
            runners = TeamModel.Instance.PlayerRunners.ToList(),
            route = selectedRoute,
            runConditions = new RunConditions { }
        });

        CutsceneUIController.toggleEvent.Invoke(false);
    }

    public void OnScrollRectValueChanged(Vector2 value)
    {
        if (canvas.enabled && selectedRoute != null && !routeMapCardScrollRect.IsDragging && routeMapCardScrollRect.velocity.sqrMagnitude < scrollRectSnapVelocityThreshold && routeMapCardScrollRect.velocity.sqrMagnitude > 10f)
        {
            routeMapCardScrollRect.StopMovement();

            int cardIndex = Mathf.RoundToInt((-routeMapCardScrollRect.content.anchoredPosition.x - routeMapCardLayoutGroup.padding.left) / (cardWidth + routeMapCardLayoutGroup.spacing));

            SelectRoute(RouteModel.Instance.Routes.First(r => r.DisplayName == activeRouteMapCards[cardIndex].RouteName));
        }
    }

    public void OnScrollRectDragEnded()
    {
        if (routeMapCardScrollRect.velocity.sqrMagnitude < scrollRectSnapVelocityThreshold)
        {
            int cardIndex = Mathf.RoundToInt((-routeMapCardScrollRect.content.anchoredPosition.x - routeMapCardLayoutGroup.padding.left) / (cardWidth + routeMapCardLayoutGroup.spacing));

            SelectRoute(RouteModel.Instance.Routes.First(r => r.DisplayName == activeRouteMapCards[cardIndex].RouteName));
        }
    }

    public void OnRosterButton()
    {
        OnToggle(false);
        CutsceneUIController.toggleEvent.Invoke(false);
        RosterUIController.toggleEvent.Invoke(true, () =>
        {
            CutsceneUIController.toggleEvent.Invoke(true);
            toggleEvent.Invoke(true);
        });
    }

    #endregion

    #region Utility Functions
    private void SelectRoute(Route r)
    {
        selectedRoute = r;

        if (selectedRoute != null)
        {
            CNExtensions.SafeStartCoroutine(this, ref scrollToggleRoutine, CNAction.ScaleCanvasObject(routeMapCardScrollRect.gameObject, GameManager.Instance.DefaultUIAnimationTime, Vector3.one));

            RouteMapCard card = activeRouteMapCards.First(rmc => rmc.RouteName == r.DisplayName);
            CNExtensions.SafeStartCoroutine(this, ref scrollRoutine, ScrollToCard(card, routeMapCardScrollRect.transform.localScale.y < 1 ? 0.01f : GameManager.Instance.DefaultUIAnimationTime));

            confirmButton.interactable = true;
        }
        else
        {
            CNExtensions.SafeStartCoroutine(this, ref scrollToggleRoutine, CNAction.ScaleCanvasObject(routeMapCardScrollRect.gameObject, GameManager.Instance.DefaultUIAnimationTime, Vector3.right));

            confirmButton.interactable = false;
        }

        routeSelectedEvent.Invoke(new RouteSelectedEvent.Context
        {
            route = r
        });
    }

    private IEnumerator ScrollToCard(RouteMapCard card, float time)
    {
        Vector2 targetPos = new Vector2(routeMapCardLayoutGroup.padding.left - card.GetComponent<RectTransform>().anchoredPosition.x, 0);

        routeMapCardScrollRect.onValueChanged.RemoveListener(OnScrollRectValueChanged);

        yield return CNAction.MoveCanvasObject(routeMapCardScrollRect.content.gameObject, time, targetPos);

        routeMapCardScrollRect.onValueChanged.AddListener(OnScrollRectValueChanged);

        routeMapCardScrollRect.StopMovement();
    }
    
    #endregion
}
