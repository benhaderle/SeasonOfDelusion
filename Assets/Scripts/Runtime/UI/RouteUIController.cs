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
    private enum State { RouteSelection = 0, EaseSelection = 1 };
    private State currentState;
    [SerializeField] private Canvas canvas;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RawImage mapDislayImage;
    [SerializeField] private Scene mapScene;
    [SerializeField] private CarouselScrollRect routeMapCardScrollRect;
    [SerializeField] private float scrollRectSnapVelocityThreshold = 10000f;
    [SerializeField] private HorizontalLayoutGroup routeMapCardLayoutGroup;
    [SerializeField] private PoolContext routeMapCardPool;
    private List<RouteMapCard> activeRouteMapCards = new();
    [SerializeField] private Button confirmButton;

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

    private void OnToggle(bool active)
    {
        if (active)
        {
            CNExtensions.SafeStartCoroutine(this, ref toggleRoutine, CNAction.FadeObject(canvas, GameManager.Instance.DefaultUIAnimationTime, canvasGroup.alpha, 1, CNEase.EaseType.Linear, true, false, true));

            Rect mapPixelRect = RectTransformUtility.PixelAdjustRect(mapDislayImage.rectTransform, canvas);
            mapPixelRect.width = mapPixelRect.width * canvas.scaleFactor;
            mapPixelRect.height = mapPixelRect.height * canvas.scaleFactor;

            Rect mapUVRect = new Rect(0, 0, mapPixelRect.width / mapDislayImage.texture.width, mapPixelRect.height / mapDislayImage.texture.height);
            mapUVRect.x = (1 - mapUVRect.width) / 2f;
            mapUVRect.y = (1 - mapUVRect.height) / 2f;
            mapDislayImage.uvRect = mapUVRect;

            Route[] routes = RouteModel.Instance.Routes.Where(r => r.saveData.data.unlocked).ToArray();
            for (int i = 0; i < routes.Length; i++)
            {
                RouteMapCard rmc = routeMapCardPool.GetPooledObject<RouteMapCard>();
                rmc.Setup(routes[i]);
                activeRouteMapCards.Add(rmc);
            }

            cardWidth = routeMapCardLayoutGroup.GetComponentInChildren<LayoutElement>().preferredWidth;
            int horizontalPadding = (int)(routeMapCardScrollRect.GetComponent<RectTransform>().rect.width - cardWidth) / 2;
            routeMapCardLayoutGroup.padding.left = horizontalPadding;
            routeMapCardLayoutGroup.padding.right = horizontalPadding;

            routeMapCardScrollRect.onValueChanged.AddListener(OnScrollRectValueChanged);

            SceneManager.LoadSceneAsync((int)mapScene, LoadSceneMode.Additive);
            SceneManager.sceneLoaded += OnMapSceneLoaded;
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

    private void OnMapSceneLoaded(UnityEngine.SceneManagement.Scene scene, LoadSceneMode loadSceneMode)
    {
        if (scene.buildIndex == (int)mapScene)
        {
            MapController.showRoutesEvent.Invoke(new MapController.ShowRoutesEvent.Context
            {
                routes = RouteModel.Instance.Routes.Where(r => r.saveData.data.unlocked).ToList()
            });

            SelectRoute(RouteModel.Instance.Routes.First(r => activeRouteMapCards[0].RouteName == r.Name));

            SceneManager.sceneLoaded -= OnMapSceneLoaded;
        }
    }

    private void OnMapUnselected()
    {
        SelectRoute(null);
    }

    private void OnRouteLineTapped(MapCameraController.RouteLineTappedEvent.Context context)
    {
        SelectRoute(RouteModel.Instance.Routes.First(r => r.Name == context.routeName));
    }

    private void SelectRoute(Route r)
    {
        selectedRoute = r;

        if (selectedRoute != null)
        {
            CNExtensions.SafeStartCoroutine(this, ref scrollToggleRoutine, CNAction.ScaleCanvasObject(routeMapCardScrollRect.gameObject, GameManager.Instance.DefaultUIAnimationTime, Vector3.one));

            RouteMapCard card = activeRouteMapCards.First(rmc => rmc.RouteName == r.Name);
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

    public void OnConfirmButton()
    {
        OnToggle(false);
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

            SelectRoute(RouteModel.Instance.Routes.First(r => r.Name == activeRouteMapCards[cardIndex].RouteName));
        }
    }

    public void OnScrollRectDragEnded()
    {
        if (routeMapCardScrollRect.velocity.sqrMagnitude < scrollRectSnapVelocityThreshold)
        {
            int cardIndex = Mathf.RoundToInt((-routeMapCardScrollRect.content.anchoredPosition.x - routeMapCardLayoutGroup.padding.left) / (cardWidth + routeMapCardLayoutGroup.spacing));

            SelectRoute(RouteModel.Instance.Routes.First(r => r.Name == activeRouteMapCards[cardIndex].RouteName));
        }
    }

    private IEnumerator ScrollToCard(RouteMapCard card, float time)
    {
        Vector2 targetPos = new Vector2(routeMapCardLayoutGroup.padding.left - card.GetComponent<RectTransform>().anchoredPosition.x, 0);

        routeMapCardScrollRect.onValueChanged.RemoveListener(OnScrollRectValueChanged);

        yield return CNAction.MoveCanvasObject(routeMapCardScrollRect.content.gameObject, time, targetPos);

        routeMapCardScrollRect.onValueChanged.AddListener(OnScrollRectValueChanged);

        routeMapCardScrollRect.StopMovement();
    }
}
