using UnityEngine;
using TMPro;
using BizarreChess.Core.Units;

namespace BizarreChess.Presentation
{
    /// <summary>
    /// Renders a single unit on the board.
    /// </summary>
    public class UnitRenderer : MonoBehaviour
    {
        [Header("Visual Components")]
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private TextMeshPro _unicodeText;
        [SerializeField] private SpriteRenderer _selectionIndicator;
        [SerializeField] private GameObject _healthBar;
        [SerializeField] private Transform _healthFill;

        [Header("Colors")]
        [SerializeField] private Color _player1Color = Color.white;
        [SerializeField] private Color _player2Color = Color.black;
        [SerializeField] private Color _selectedColor = new Color(1f, 1f, 0f, 0.5f);
        [SerializeField] private Color _damagedColor = Color.red;

        [Header("Animation")]
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private float _bounceHeight = 0.2f;

        public int UnitId { get; private set; }
        public System.Action OnClicked;

        private UnitState _currentState;
        private UnitDefinition _definition;
        private Vector3 _startPosition;
        private Vector3 _targetPosition;
        private bool _isMoving;
        private bool _isSelected;
        private float _moveProgress;

        public void Initialize(UnitState state, UnitDefinition definition, Vector3 position)
        {
            UnitId = state.UnitId;
            _currentState = state;
            _definition = definition;
            _targetPosition = position;
            transform.position = position + Vector3.up * 0.5f;

            // Auto-find components if not assigned (for dynamically created units)
            if (_unicodeText == null)
                _unicodeText = GetComponentInChildren<TextMeshPro>();
            if (_spriteRenderer == null)
                _spriteRenderer = GetComponent<SpriteRenderer>();

            UpdateVisuals();
        }

        public void UpdateState(UnitState state)
        {
            _currentState = state;
            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            // Set color based on player - white pieces light, black pieces dark
            Color playerColor = _currentState.OwnerId == 0 ? Color.white : new Color(0.15f, 0.15f, 0.15f);
            Color outlineColor = _currentState.OwnerId == 0 ? Color.black : Color.white;

            // Use Unicode text for placeholder
            if (_unicodeText != null)
            {
                // Try Unicode first, fallback to letter if font doesn't support it
                char pieceChar = _definition.GetUnicode(_currentState.OwnerId);
                string displayText = ChessUnicode.GetPieceLetter(_definition.PieceType);
                
                // Check if font has the Unicode character
                if (_unicodeText.font != null && _unicodeText.font.HasCharacter(pieceChar))
                {
                    displayText = pieceChar.ToString();
                }
                
                _unicodeText.text = displayText;
                _unicodeText.fontSize = 5;
                _unicodeText.color = playerColor;
                _unicodeText.outlineWidth = 0.15f;
                _unicodeText.outlineColor = outlineColor;
                _unicodeText.fontStyle = TMPro.FontStyles.Bold;
            }

            // Or use sprite if available
            if (_spriteRenderer != null && _definition.GetSprite(_currentState.OwnerId) != null)
            {
                _spriteRenderer.sprite = _definition.GetSprite(_currentState.OwnerId);
                _spriteRenderer.color = playerColor;
                if (_unicodeText != null) _unicodeText.enabled = false;
            }

            // Update health bar
            if (_healthBar != null && _healthFill != null)
            {
                float healthPercent = (float)_currentState.CurrentHealth / _currentState.MaxHealth;
                _healthFill.localScale = new Vector3(healthPercent, 1, 1);
                
                // Show health bar only if damaged
                _healthBar.SetActive(healthPercent < 1f);
            }

            // Selection indicator
            if (_selectionIndicator != null)
            {
                _selectionIndicator.enabled = _isSelected;
                _selectionIndicator.color = _selectedColor;
            }

            // Dead units fade out
            if (!_currentState.IsAlive)
            {
                SetAlpha(0.3f);
            }
        }

        public void SetSelected(bool selected)
        {
            _isSelected = selected;
            
            if (_selectionIndicator != null)
            {
                _selectionIndicator.enabled = selected;
            }

            // Visual feedback
            transform.localScale = selected ? Vector3.one * 1.1f : Vector3.one;
        }

        public void MoveTo(Vector3 newPosition)
        {
            _startPosition = transform.position;
            _targetPosition = newPosition + Vector3.up * 0.5f;
            _isMoving = true;
            _moveProgress = 0f;
            
            Debug.Log($"[UnitRenderer] MoveTo: from {_startPosition} to {_targetPosition}");
        }

        private void Update()
        {
            if (_isMoving)
            {
                _moveProgress += Time.deltaTime * _moveSpeed;
                
                if (_moveProgress >= 1f)
                {
                    transform.position = _targetPosition;
                    _isMoving = false;
                    Debug.Log($"[UnitRenderer] Movement complete at {_targetPosition}");
                }
                else
                {
                    // Lerp from start to target position with bounce
                    Vector3 currentPos = Vector3.Lerp(_startPosition, _targetPosition, _moveProgress);
                    float bounce = Mathf.Sin(_moveProgress * Mathf.PI) * _bounceHeight;
                    currentPos.y += bounce;
                    transform.position = currentPos;
                }
            }
        }

        private void SetAlpha(float alpha)
        {
            if (_spriteRenderer != null)
            {
                var color = _spriteRenderer.color;
                color.a = alpha;
                _spriteRenderer.color = color;
            }

            if (_unicodeText != null)
            {
                var color = _unicodeText.color;
                color.a = alpha;
                _unicodeText.color = color;
            }
        }

        public void PlayAttackAnimation()
        {
            // Simple scale punch animation
            StartCoroutine(AttackAnimationCoroutine());
        }

        private System.Collections.IEnumerator AttackAnimationCoroutine()
        {
            Vector3 originalScale = Vector3.one;
            Vector3 punchScale = Vector3.one * 1.3f;

            float duration = 0.1f;
            float elapsed = 0f;

            // Scale up
            while (elapsed < duration)
            {
                transform.localScale = Vector3.Lerp(originalScale, punchScale, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Scale back down
            elapsed = 0f;
            while (elapsed < duration)
            {
                transform.localScale = Vector3.Lerp(punchScale, originalScale, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.localScale = originalScale;
        }

        public void PlayDeathAnimation()
        {
            StartCoroutine(DeathAnimationCoroutine());
        }

        private System.Collections.IEnumerator DeathAnimationCoroutine()
        {
            float duration = 0.5f;
            float elapsed = 0f;
            Vector3 startScale = transform.localScale;

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
                SetAlpha(1f - t);
                elapsed += Time.deltaTime;
                yield return null;
            }

            gameObject.SetActive(false);
        }

        // Click handling moved to InputHandler (OnMouseDown uses old Input system)
    }
}

