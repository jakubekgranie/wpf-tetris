# wpf-tetris
<i>(C) jakubekgranie 2024. All rights reserved.</i>
<h3>1. Introduction</h3>
<p>"wpf-tetris" is a Tetris game implementation made by <a href="https://github.com/jakubekgranie">jakubekgranie on GitHub</a>, issued as a school assignment. The project features <b>C#</b> and <b>XAML</b> languages. The user interface framework used is WPF (Windows Presentation Foundation).</p>
<h3>2. The game</h3>
<p>The objective of the player is to <i>gather as many points as possible until the game ends by overflow</i>. On a grid of chosen dimensions, they must <i>align the incoming shapes into horizontal lines</i> by <i>rotating</i>, <i>swapping</i> and <i>vertical movement</i>. The more score you have, the harder the game becomes, but fortune favors the bold...</p>
<h3>3. The level system</h3>
<ol>
  <li>
      <h4>The formula</h4>
      <p>The points are added using the formula below:</p>
      <kbd>points = previous + amount * horizontalMultiplier * level</kbd>,
      <p>where <kbd>previous</kbd> equals the past total score, <kbd>amount</kbd> is the unparsed amount of points, <kbd>horizontalMultiplier</kbd> is a temporary multiplier growing with consequent horizontal lines detected in one scan and <kbd>level</kbd> is responsible for level achievement gratification.</p>
  </li>
  <li>
      <h4>Acquisition</h4>
      <p>How to get points (unparsed):</p>
      <ul>
        <li>horizontal line creation - <b>500 points</b></li>
        <li>Voluntary block descent - <b>2 points</b></li>
        <li>Voluntary block descent (full) - <b>2 * empty spaces traversed points</b></li>
      </ul>
  </li>
  <li>
      <h4>Level thresholds</h4>
      <p>Each level threshold can be calculated using the formula below:</p>
      <kbd>threshold = previous + n(1000 + 200 * level)</kbd>,
      <p>where <kbd>previous</kbd> is the previous threshold, <kbd>n</kbd> stands for the number of iterations and <kbd>level</kbd> is self-explanatory.</p>
  </li>
  <li>
      <h4>Timer intervals</h4>
      <p>Timer intervals are relative to the current level, and trigger automatic block descent, see the formula below:</p>
      <kbd>time = 1250 - 150 * level</kbd>,
      <p>where <kbd>level</kbd> is self-explanatory.
      <br>
      The unit of time is <b>ms</b> (<b>milliseconds</b>).</p>
  </li>
</ol>
<h3>4. Controls</h3>
<ul>
  <li><kbd>A</kbd> - <i>Move to the left</i></li>
  <li><kbd>D</kbd> - <i>Move to the right</i></li>
  <li><kbd>-></kbd> - <i>Rotate to the right</i></li>
  <li><kbd><-</kbd> - <i>Rotate to the left</i></li>
  <li><kbd>S</kbd> - <i><b>Voluntary block descent</b></i></li>
  <li><kbd>Space</kbd> - <i><b>Voluntary block descent (full)</b></i></li>
  <li><kbd>Z</kbd> - <i>Swap (TBD)</i></li>
  <li><kbd>F5</kbd> - <i>View save manager</i></li>
  <li><kbd>F9</kbd> - <i>Reset</i></li>
</ul>
<h3>5. Internal documentation excerpts</h3>
<p>TBD</p>
<h3>6. License</h3>
<i>See <a href="https://github.com/jakubekgranie/wpf-tetris/blob/master/LICENSE.txt">LICENSE.txt</a>.
<br>
I, the creator of the project, reserve the right for proper notification of this project's external usage and denial of such.</i>
<hr>
<sub>Visit <a href="https://github.com/jakubekgranie">jakubekgranie on github</a> for more awesome solutions!</sub>
