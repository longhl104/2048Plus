using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DG.Tweening;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Assets.Scripts.Utils;
using Assets.Scripts;

public class GameManager : SwipeDetection
{
    [SerializeField] private int _width = 4;
    [SerializeField] private int _height = 4;
    [SerializeField] private Node _nodePrefab;
    [SerializeField] private Block _blockPrefab;
    [SerializeField] private ExplosiveBlock _explosiveBlockPrefab;
    [SerializeField] private SpriteRenderer _boardPrefab;
    [SerializeField] private List<BlockType> _types;
    [SerializeField] private float _travelTime = .2f;
    [SerializeField] private bool _isTest = false;
    [SerializeField] private TextMeshProUGUI _scoreText;
    [SerializeField] private NumberCounterUpdater _scoreCounter;
    [SerializeField] private TextMeshProUGUI _highScoreText;
    [SerializeField] private NumberCounterUpdater _highScoreCounter;
    [SerializeField] private GameObject _gamePanel;
    [SerializeField] private GameObject _restartButton;
    [SerializeField] private GameObject _cheeringText;
    [SerializeField] private AudioSource _gameOverSound;
    [SerializeField] private AudioSource _explosiveSound;
    [SerializeField] private RewardedAdsButton _rewindButton;
    [SerializeField] private AudioSource _mergeSound;
    [SerializeField] private float _idleTime = 15f;
    [SerializeField] private SwipeInstruction _swipeInstruction;

    private List<Node> _nodes;
    private List<BaseBlock> _blocks;
    private GameState _state;
    private int _round;
    private BlockType GetBlockTypeByValue(int value) => _types.First(t => t.Value == value);
    private Board _board;
    private StoredBoards _storedBoards;
    private bool _isCheeringTextPlaying = false;
    private float _lastIdleTime;
    private readonly string[] _cheeringWords = new string[] {
        "Nice!",
        "Amazing!",
        "Good Job!",
        "Brilliant!",
        "Fabulous!",
        "Fantastic!",
        "Great!",
        "Impressive!",
        "Marvellous!",
        "Outstanding!",
        "Superb!",
        "Smahsing!",
        "Terrific!",
        "Wonderful!"
    };

    private readonly string[] _explosingWords = new string[] {
        "BOOM!",
        "BANG!",
    };


    private void AnimateButtons()
    {
        var sequence = DOTween.Sequence();
        sequence.Append(_restartButton.transform.DOMoveY(_restartButton.transform.position.y + 10, 0.5f))
            .Join(_rewindButton.transform.DOScale(1.1f, 0.5f))
            .SetLoops(-1, LoopType.Yoyo)
            ;
    }

    void Start()
    {
        AnimateButtons();
        ChangeState(GameState.GenerateLevel);
    }

    private void PlayCheeringText(bool isExplosive)
    {
        if (!_isCheeringTextPlaying)
        {
            _isCheeringTextPlaying = true;
            float duration = 0.7f;
            string text;
            if (isExplosive)
                text = _explosingWords.OrderBy(x => UnityEngine.Random.value).First();
            else
                text = _cheeringWords.OrderBy(x => UnityEngine.Random.value).First();

            _cheeringText.GetComponentInChildren<TextMeshPro>().SetText(text);
            var textObject = Instantiate(_cheeringText, new Vector2(1.5f, 1.5f), Quaternion.identity);

            var sequence = DOTween.Sequence();
            if (!isExplosive)
            {
                sequence.Append(textObject.transform.DOMove(new Vector2(1.5f, 4f), duration));
                sequence.Join(textObject.transform.DOScale(new Vector2(4.5f, 4.5f), duration));
                sequence.Append(textObject.transform.DOShakeScale(1));
            }
            else
            {
                sequence.Append(textObject.transform.DOScale(new Vector2(4.5f, 4.5f), 0.1f));
                sequence.Join(textObject.transform.DOMove(new Vector2(1.5f, 4f), 0.1f));
                sequence.Append(textObject.transform.DOShakeScale(duration));
            }

            sequence.OnComplete(() =>
            {
                Destroy(textObject);
                _isCheeringTextPlaying = false;
            });
        }
    }

    private void ChangeState(GameState newState)
    {
        _state = newState;
        switch (newState)
        {
            case GameState.GenerateLevel:
                GenerateGrid();
                break;

            case GameState.SpawningBlocks:
                SpawnBlocks();
                break;

            case GameState.Explosing:
                break;

            case GameState.WaitingInput:
                WaitForInput();
                break;

            case GameState.Moving:
                break;

            case GameState.Win:
                break;

            case GameState.Lose:
                ShowGameOver();
                break;

            default:
                break;
        }
    }

    private void WaitForInput()
    {

    }

    private void ShowGameOver()
    {
        _gameOverSound.Play();
        _gamePanel.GetComponent<Image>().color = new Color(0, 0, 0, 0.75f);
        ShowButtonsWhenLose(true);
    }

    private List<Vector2> GetAvailableSwipeDirections()
    {
        Vector2[] directions = new Vector2[] { Vector2.up, Vector2.down, Vector2.left, Vector2.right };

        return directions.Where(direction =>
        {
            return _blocks.Any(block =>
            {
                if (block is ExplosiveBlock)
                    return false;

                Node otherNode = GetNodeAtPosition(block.Pos + direction);
                if (otherNode == null)
                {
                    return false;
                }

                return otherNode.OccupiedBlock == null
                    || block.Value == otherNode.OccupiedBlock.Value
                    ;
            });
        }).ToList();
    }

    private void Update()
    {
        if (_state == GameState.WaitingInput)
        {
            if (!_swipeInstruction.IsPlaying() && Time.time - _lastIdleTime > _idleTime)
            {
                var directions = GetAvailableSwipeDirections();
                _swipeInstruction.Swipe(directions.OrderBy(x => UnityEngine.Random.value).First());
            }
        }

        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            SwipeUpEvent();
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            SwipeDownEvent();
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            SwipeLeftEvent();
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            SwipeRightEvent();
        }
    }

    private void SetScore(int score)
    {
        _board.Score = score;
        if (_round <= 1) _scoreCounter.SetValueInstantly(PlayerPrefs.HasKey(StorageKeys.LAST_SCORE) ? PlayerPrefs.GetInt(StorageKeys.LAST_SCORE) : 0);
        _scoreCounter.Value = score;

        if (!PlayerPrefs.HasKey(StorageKeys.HIGH_SCORE))
        {
            PlayerPrefs.SetInt(StorageKeys.HIGH_SCORE, score);
        }

        int highScore = PlayerPrefs.GetInt(StorageKeys.HIGH_SCORE);
        if (_board.Score > highScore)
        {
            PlayerPrefs.SetInt(StorageKeys.HIGH_SCORE, score);
            highScore = score;
        }

        _highScoreCounter.Value = highScore;
        PlayerPrefs.SetInt(StorageKeys.LAST_SCORE, score);
    }

    private void ShowButtonsWhenLose(bool shown)
    {
        _restartButton.gameObject.SetActive(shown);

        if (shown)
        {
            _rewindButton.LoadAd();
            _rewindButton.gameObject.SetActive(_storedBoards.GetRewindedBoard() != null);
        }
        else _rewindButton.gameObject.SetActive(false);
    }

    public void Rewind()
    {
        if (_storedBoards == null)
            return;

        Board board = _storedBoards.GetRewindedBoard();
        if (board == null)
            return;

        StorageHandler.SaveData(board, StorageKeys.BOARD);
        SceneManager.LoadScene("Game");
    }

    private void GenerateGrid()
    {
        _storedBoards = new StoredBoards();
        //_highScoreText.text = "High Score: " + PlayerPrefs.GetInt(StorageKeys.HIGH_SCORE, 0);
        _gamePanel.GetComponent<Image>().color = new Color(0, 0, 0, 0);
        ShowButtonsWhenLose(false);
        _round = 0;
        _nodes = new List<Node>();
        _blocks = new List<BaseBlock>();
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                var node = Instantiate(_nodePrefab, new Vector2(x, y), Quaternion.identity);
                _nodes.Add(node);
            }
        }

        if (_isTest)
        {
            SpawnBlock(GetNodeAtPosition(new Vector2(0, 0)), 2, _blockPrefab);
            SpawnBlock(GetNodeAtPosition(new Vector2(0, 1)), 4, _blockPrefab);
            SpawnBlock(GetNodeAtPosition(new Vector2(0, 2)), 8, _blockPrefab);
            SpawnBlock(GetNodeAtPosition(new Vector2(0, 3)), 16, _blockPrefab);
            SpawnBlock(GetNodeAtPosition(new Vector2(1, 3)), 32, _blockPrefab);
            SpawnBlock(GetNodeAtPosition(new Vector2(2, 3)), 64, _blockPrefab);
            SpawnBlock(GetNodeAtPosition(new Vector2(3, 2)), 256, _blockPrefab);
            SpawnBlock(GetNodeAtPosition(new Vector2(3, 1)), 512, _blockPrefab);
            SpawnBlock(GetNodeAtPosition(new Vector2(3, 0)), 1024, _blockPrefab);
            SpawnBlock(GetNodeAtPosition(new Vector2(2, 0)), 2, _blockPrefab);
            SpawnBlock(GetNodeAtPosition(new Vector2(1, 0)), 4096, _blockPrefab);
            SpawnBlock(GetNodeAtPosition(new Vector2(1, 1)), 2, _blockPrefab);
            SpawnBlock(GetNodeAtPosition(new Vector2(2, 2)), 8, _blockPrefab);
            SpawnBlock(GetNodeAtPosition(new Vector2(1, 2)), 16, _blockPrefab);
            SpawnBlock(GetNodeAtPosition(new Vector2(2, 1)), 2, _explosiveBlockPrefab);
        }

        var center = new Vector2((float)_width / 2 - 0.5f, (float)_height / 2 - 0.5f);
        var board = Instantiate(_boardPrefab, center, Quaternion.identity);
        board.size = new Vector2(_width, _height);
        Camera.main.transform.position = new Vector3(center.x, center.y, -10);
        ChangeState(GameState.SpawningBlocks);
    }

    private IEnumerable<Node> GetFreeNodes()
    {
        return _nodes.Where(n => n.OccupiedBlock == null).OrderBy(x => UnityEngine.Random.value);
    }

    private bool IsBoardValid()
    {
        if (_board == null)
            return false;

        if (_board.Blocks.Count > 0
            && _board.ExplosiveValue >= 2 && IsPowerOfTwo(_board.ExplosiveValue)
            )
        {
            return true;
        }

        StorageHandler.DeleteData(StorageKeys.BOARD);
        return false;

        static bool IsPowerOfTwo(int x)
        {
            return (x & (x - 1)) == 0;
        }
    }

    private void SpawnBlocks()
    {
        _mergeSound.Play();
        var freeNodes = GetFreeNodes();
        int normalBlockValue = UnityEngine.Random.value > 0.9f ? 4 : 2;
        if (freeNodes.Count() > 0)
        {
            if (_round++ == 0)
            {
                _board = (Board)StorageHandler.LoadData(StorageKeys.BOARD);
                if (!IsBoardValid())
                {
                    _board = new Board();
                    SetScore(0);
                    _board.ExplosiveValue = 2;
                    var freeNode = freeNodes.First();
                    SpawnBlock(freeNode, normalBlockValue, _blockPrefab);

                    if (freeNodes.Count() > 1)
                    {
                        SpawnBlock(freeNodes.Skip(1).First(), _board.ExplosiveValue, _explosiveBlockPrefab);
                    }
                }
                else
                {
                    SetScore(_board.Score);
                    foreach (var block in _board.Blocks)
                    {
                        SpawnBlock(_nodes.First(n => (int)n.Pos.x == block.X && (int)n.Pos.y == block.Y), block.Value, block.IsExplosive ? _explosiveBlockPrefab : _blockPrefab);
                    }
                }
            }
            else
            {
                SpawnBlock(freeNodes.First(), normalBlockValue, _blockPrefab);
                if (freeNodes.Count() > 1 && _blocks.All(b => b is Block))
                {
                    _board.ExplosiveValue *= 2;
                    SpawnBlock(freeNodes.Skip(1).First(), _board.ExplosiveValue, _explosiveBlockPrefab);
                }
            }
        }

        SaveBlocksToBoard();
        _storedBoards.SetBoard(_board.DeepClone());

        if (IsLoseState())
        {
            ChangeState(GameState.Lose);
            return;
        }

        ChangeState(GameState.WaitingInput);
    }

    void SpawnBlock(Node node, int value, BaseBlock blockPrefab)
    {
        var block = Instantiate(blockPrefab, node.Pos, Quaternion.identity);
        block.Init(GetBlockTypeByValue(value));
        block.SetBlock(node);
        _blocks.Add(block);
    }

    private bool IsLoseState()
    {
        if (_blocks.Count != _nodes.Count)
            return false;

        return _blocks.All(block =>
        {
            var adjacentBlocks = _blocks
                .Where(b => (int)(Mathf.Abs(b.Pos.x - block.Pos.x) + Mathf.Abs(b.Pos.y - block.Pos.y)) == 1)
                .ToList();

            return adjacentBlocks.All(b => b.Value != block.Value);
        });
    }

    void Shift(Vector2 dir)
    {
        if (_state != GameState.WaitingInput)
            return;

        _lastIdleTime = Time.time;
        _swipeInstruction.Stop();
        ChangeState(GameState.Moving);
        IEnumerable<BaseBlock> orderedBlocks = _blocks.Where(b => b is Block);
        if (dir == Vector2.up)
            orderedBlocks = orderedBlocks.OrderBy(b => b.Pos.x).ThenBy(b => -b.Pos.y);
        else if (dir == Vector2.down)
            orderedBlocks = orderedBlocks.OrderBy(b => b.Pos.x).ThenBy(b => b.Pos.y);
        else if (dir == Vector2.left)
            orderedBlocks = orderedBlocks.OrderBy(b => b.Pos.y).ThenBy(b => b.Pos.x);
        else // dir == Vector2.right
            orderedBlocks = orderedBlocks.OrderBy(b => b.Pos.y).ThenBy(b => -b.Pos.x);

        var hasMovedBlocks = false;
        foreach (var block in orderedBlocks)
        {
            var next = block.Node;
            do
            {
                block.SetBlock(next);
                var possibleNode = GetNodeAtPosition(next.Pos + dir);
                if (possibleNode != null
                    && block is Block
                    )
                {
                    // block in possible node <- current block
                    if (possibleNode.OccupiedBlock != null && possibleNode.OccupiedBlock.CanMerge(block.Value))
                    {
                        hasMovedBlocks = true;
                        block.MergeBlock(possibleNode.OccupiedBlock);
                    }
                    else if (possibleNode.OccupiedBlock == null)
                    {
                        hasMovedBlocks = true;
                        next = possibleNode;
                    }
                }
            } while (next != block.Node);
        }

        var sequence = DOTween.Sequence();
        foreach (var block in orderedBlocks)
        {
            var movePoint = block.MergingBlock != null ? block.MergingBlock.Node.Pos : block.Node.Pos;
            sequence.Insert(0, block.transform.DOMove(movePoint, _travelTime));
        }

        sequence.OnComplete(() =>
        {
            foreach (var block in orderedBlocks.Where(b => b.MergingBlock != null))
            {
                MergeBlocks(block.MergingBlock, block);
            }

            if (_state != GameState.Explosing)
                ChangeState(hasMovedBlocks ? GameState.SpawningBlocks : GameState.WaitingInput);
        });
    }

    void MergeBlocks(BaseBlock baseBlock, BaseBlock mergingBlock)
    {
        if (baseBlock is Block)
        {
            SetScore(_board.Score + baseBlock.Value * 2);
            SpawnBlock(baseBlock.Node, baseBlock.Value * 2, _blockPrefab);
            RemoveBlock(baseBlock);
            RemoveBlock(mergingBlock);
        }
        else
        {
            PlayCheeringText(false);
            if (baseBlock.Value / 2 > 1)
            {
                SetScore(_board.Score + baseBlock.Value * 2);
                SpawnBlock(baseBlock.Node, baseBlock.Value / 2, _explosiveBlockPrefab);
                RemoveBlock(baseBlock);
                RemoveBlock(mergingBlock);
            }
            else // Destroy all surrouding blocks 
            {
                _explosiveSound.Play();
                RemoveBlock(mergingBlock);
                ExplosiveBlock eb = (ExplosiveBlock)baseBlock;
                ChangeState(GameState.Explosing);
                eb.TriggerExplosion(() =>
                {
                    PlayCheeringText(true);
                    var totalScore = 0;
                    foreach (var node in _nodes.Where(n => n.OccupiedBlock != null
                         && Math.Max(Math.Abs(n.Pos.x - baseBlock.Pos.x), Math.Abs(n.Pos.y - baseBlock.Pos.y)) <= 1
                        ))
                    {
                        totalScore += node.OccupiedBlock.Value;
                        RemoveBlock(node.OccupiedBlock);
                    }

                    SetScore(_board.Score + totalScore);
                    RemoveBlock(baseBlock);
                    ChangeState(GameState.SpawningBlocks);
                });
            }
        }
    }

    void RemoveBlock(BaseBlock block)
    {
        _blocks.Remove(block);
        if (block is Block b)
        {
            StartCoroutine(b.Destroy());
        }
        else
        {
            if (block.gameObject != null)
                Destroy(block.gameObject);
        }
    }

    Node GetNodeAtPosition(Vector2 pos)
    {
        return _nodes.FirstOrDefault(n => n.Pos == pos);
    }

    protected override void SwipeUpEvent()
    {
        Shift(Vector2.up);
    }

    protected override void SwipeDownEvent()
    {
        Shift(Vector2.down);
    }

    protected override void SwipeLeftEvent()
    {
        Shift(Vector2.left);
    }

    protected override void SwipeRightEvent()
    {
        Shift(Vector2.right);
    }

    public void Restart()
    {
        while (_blocks.Count > 0)
        {
            RemoveBlock(_blocks.First());
        }

        StorageHandler.DeleteData(StorageKeys.BOARD);
        ChangeState(GameState.GenerateLevel);
    }

    private void SaveBlocksToBoard()
    {
        if (_board == null)
            return;

        _board.Blocks = _blocks.Select(block => new Board.Block()
        {
            X = (int)block.Pos.x,
            Y = (int)block.Pos.y,
            Value = block.Value,
            IsExplosive = block is ExplosiveBlock,
        }).ToList();
    }

    public void OnBackButtonClicked()
    {
        if (_state != GameState.WaitingInput)
            return;

        if (_board != null)
        {
            SaveBlocksToBoard();
            StorageHandler.SaveData(_board, StorageKeys.BOARD);
        }

        SceneManager.LoadScene("MainMenu");
    }
}

[Serializable]
public struct BlockType
{
    public int Value;
    public Color Color;
}

public enum GameState
{
    GenerateLevel,
    SpawningBlocks,
    Explosing,
    WaitingInput,
    Moving,
    Win,
    Lose,
}

[Serializable]
public class Board
{
    [Serializable]
    public class Block
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Value { get; set; }
        public bool IsExplosive { get; set; }

        public Block DeepClone()
        {
            return new Block()
            {
                X = X,
                Y = Y,
                Value = Value,
                IsExplosive = IsExplosive,
            };
        }
    }

    public List<Block> Blocks { get; set; } = new List<Block>();

    public int Score { get; set; }
    public int ExplosiveValue { get; set; }

    public Board DeepClone()
    {
        return new Board()
        {
            Blocks = Blocks.Select(b => b.DeepClone()).ToList(),
            Score = Score,
            ExplosiveValue = ExplosiveValue,
        };
    }
}

[Serializable]
public class StoredBoards
{
    public Queue<Board> Boards { get; set; } = new Queue<Board>();
    public Board CurrentBoard { get; set; }

    public void SetBoard(Board board, bool reset = false)
    {
        if (reset)
            Boards.Clear();

        CurrentBoard = board;
        Boards.Enqueue(board);
        if (Boards.Count == 6)
        {
            Boards.Dequeue();
        }
    }

    public Board GetRewindedBoard()
    {
        if (Boards.Count != 5)
            return null;

        return Boards.Peek();
    }
}