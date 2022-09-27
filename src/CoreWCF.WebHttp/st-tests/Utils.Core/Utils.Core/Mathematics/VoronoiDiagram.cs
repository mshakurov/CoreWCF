using System;
using System.Collections.Generic;
using System.Windows;

using Microsoft.SqlServer.Types;

using ST.Utils.Attributes;

namespace ST.Utils.Mathematics
{
  /// <summary>
  /// Класс, позволяющий строить диаграммы Вороного.
  /// </summary>
  public sealed class VoronoiDiagram
  {
    #region .Constants
    private const double EPSILON = 1e-9;
    private const double EPSILON_2 = 2e-12;
    #endregion

    #region .Fields
    private readonly Box _box;

    private readonly int _precision;

    private readonly RedBlackTree<BeachSection> _beachLine = new RedBlackTree<BeachSection>();

    private readonly RedBlackTree<CircleEvent> _circleEvents = new RedBlackTree<CircleEvent>();

    private readonly List<Edge> _edges = new List<Edge>();

    private Cell[] _cells;

    private CircleEvent _firstCircleEvent;
    #endregion

    #region .Ctor
    /// <summary>
    /// Конструктор.
    /// </summary>
    /// <param name="box">Прямоугольник, ограничивающий построение диаграммы.</param>
    /// <param name="precision">Точность построения диаграммы (кол-во знаков посля запятой).</param>
    public VoronoiDiagram(Box box, [Range(0, 6)] int precision = 0)
    {
      _precision = precision;

      _box = box;
    }
    #endregion

    #region AddBeachsection
    private void AddBeachsection(Cell cell)
    {
      var x = cell.X;
      var directrix = cell.Y;

      // Find the left and right beach sections which will surround the newly created beach section.
      // This loop is one of the most often executed, hence we expand in-place the comparison-against-epsilon calls.

      BeachSection lArc = null;
      BeachSection rArc = null;

      var node = _beachLine.Root;

      while (node != null)
      {
        var dxl = GetLeftBreakPoint(node, directrix) - x;

        if (dxl > EPSILON) // x lessThanWithEpsilon xl => falls somewhere before the left edge of the beachsection.
          // if( node.LeftNode == null ) // This case should never happen.
          // {
          //   rArc = node.LeftNode;
          //
          //   break;
          //  }
          node = node.LeftNode;
        else
        {
          var dxr = x - GetRightBreakPoint(node, directrix);

          if (dxr > EPSILON) // x greaterThanWithEpsilon xr => falls somewhere after the right edge of the beachsection.
          {
            if (node.RightNode == null)
            {
              lArc = node;

              break;
            }

            node = node.RightNode;
          }
          else
          {
            if (dxl > -EPSILON) // x equalWithEpsilon xl => falls exactly on the left edge of the beachsection.
            {
              lArc = node.PreviousNode;
              rArc = node;
            }
            else
              if (dxr > -EPSILON) // x equalWithEpsilon xr => falls exactly on the right edge of the beachsection.
            {
              lArc = node;
              rArc = node.NextNode;
            }
            else // falls exactly somewhere in the middle of the beachsection
              lArc = rArc = node;

            break;
          }
        }
      }

      // At this point, keep in mind that lArc and/or rArc could be null.

      // Create a new beach section object for the cell and add it to RB-tree.

      var newArc = new BeachSection(cell);

      _beachLine.InsertSuccessor(lArc, newArc);

      // Cases:

      // [null, null]
      // Least likely case: new beach section is the first beach section on the beachline.
      // This case means: no new transition appears; no collapsing beach section; new beachsection become root of the RB-tree.
      if (lArc == null && rArc == null)
        return;

      // [lArc, rArc] where lArc == rArc
      // Most likely case: new beach section split an existing beach section.
      // This case means: one new transition appears; the left and right beach section might be collapsing as a result; two new nodes added to the RB-tree.
      if (lArc == rArc)
      {
        // Invalidate circle event of split beach section.
        DetachCircleEvent(lArc);

        // Split the beach section into two separate beach sections.

        rArc = new BeachSection(lArc.Cell);

        _beachLine.InsertSuccessor(newArc, rArc);

        // Since we have a new transition between two beach sections, a new edge is born.
        newArc.Edge = rArc.Edge = CreateEdge(lArc.Cell, newArc.Cell, null, null);

        // Check whether the left and right beach sections are collapsing and if so create circle events, to be notified when the point of collapse is reached.

        AttachCircleEvent(lArc);
        AttachCircleEvent(rArc);

        return;
      }

      // [lArc, null]
      // Even less likely case: new beach section is the *last* beach section on the beachline - this can happen *only* if *all* the previous beach
      // sections currently on the beachline share the same y value as the new beach section.
      // This case means: one new transition appears; no collapsing beach section as a result; new beach section become right-most node of the RB-tree.
      if (lArc != null && rArc == null)
      {
        newArc.Edge = CreateEdge(lArc.Cell, newArc.Cell, null, null);

        return;
      }

      // [null, rArc]
      // Impossible case: because cells are strictly processed from top to bottom, and left to right, which guarantees that there will always be a beach section
      // on the left - except of course when there are no beach section at all on  the beach line, which case was handled above.
      // if( lArc == null && rArc != null )
      // {
      //   throw new InvalidOperationException();
      // }

      // [lArc, rArc] where lArc != rArc
      // Somewhat less likely case: new beach section falls *exactly* in between two existing beach sections.
      // This case means: one transition disappears; two new transitions appear; the left and right beach section might be collapsing as a result; only one new node added to the RB-tree.
      if (lArc != rArc)
      {
        // Invalidate circle events of left and right cells.

        DetachCircleEvent(lArc);
        DetachCircleEvent(rArc);

        // An existing transition disappears, meaning a vertex is defined at the disappearance point. Since the disappearance is caused by the new beachsection, the
        // vertex is at the center of the circumscribed circle of the left, new and right beachsections.
        // http://mathforum.org/library/drmath/view/55002.html (except that the origin at A was brouht to simplify calculation).
        var lCell = lArc.Cell;
        var ax = lCell.X;
        var ay = lCell.Y;
        var bx = cell.X - ax;
        var by = cell.Y - ay;
        var rCell = rArc.Cell;
        var cx = rCell.X - ax;
        var cy = rCell.Y - ay;
        var d = 2.0 * (bx * cy - by * cx);
        var hb = bx * bx + by * by;
        var hc = cx * cx + cy * cy;

        var vertex = new Vertex((cy * hb - by * hc) / d + ax, (bx * hc - cx * hb) / d + ay);

        // One transition disappear.
        SetEdgeStart(rArc.Edge, lCell, rCell, vertex);

        // Two new transitions appear at the new vertex location.

        newArc.Edge = CreateEdge(lCell, cell, null, vertex);
        rArc.Edge = CreateEdge(cell, rCell, null, vertex);

        // Check whether the left and right beach sections are collapsing and if so create circle events, to handle the point of collapse.

        AttachCircleEvent(lArc);
        AttachCircleEvent(rArc);

        return;
      }
    }
    #endregion

    #region AttachCircleEvent
    private void AttachCircleEvent(BeachSection arc)
    {
      var lArc = arc.PreviousNode;
      var rArc = arc.NextNode;

      if (lArc == null || rArc == null) // Does that ever happen?
        return;

      var lCell = lArc.Cell;
      var cCell = arc.Cell;
      var rCell = rArc.Cell;

      if (lCell == rCell) // If cell of left beachsection is same as cell of right beachsection, there can't be convergence.
        return;

      // Find the circumscribed circle for the three cells associated with the beachsection triplet. It is more efficient to calculate in-place
      // rather than getting the resulting circumscribed circle from an object returned by calling Voronoi.circumcircle().
      // http://mathforum.org/library/drmath/view/55002.html (except that the origin at cCell was brought to simplify calculations).
      // The bottom-most part of the circumcircle is our Fortune 'circle event', and its center is a vertex potentially part of the final Voronoi diagram.
      var bx = cCell.X;
      var by = cCell.Y;
      var ax = lCell.X - bx;
      var ay = lCell.Y - by;
      var cx = rCell.X - bx;
      var cy = rCell.Y - by;

      // If points l->c->r are clockwise, then center beach section does not collapse, hence it can't end up as a vertex (we reuse 'd' here, which
      // sign is reverse of the orientation, hence we reverse the test). // http://en.wikipedia.org/wiki/Curve_orientation#Orientation_of_a_simple_polygon
      // Nasty finite precision error which caused circumcircle() to return infinites: 1e-12 seems to fix the problem.
      var d = 2.0 * (ax * cy - ay * cx);

      if (d >= -EPSILON_2)
        return;

      var ha = ax * ax + ay * ay;
      var hc = cx * cx + cy * cy;
      var x = (cy * ha - ay * hc) / d;
      var y = (ax * hc - cx * ha) / d;
      var ycenter = y + by;

      // Important: ybottom should always be under or at sweep, so no need to waste CPU cycles by checking.

      var circleEvent = new CircleEvent(arc, cCell, x + bx, ycenter + Math.Sqrt(x * x + y * y), ycenter);

      arc.CircleEvent = circleEvent;

      // Find insertion point in RB-tree: circle events are ordered from smallest to largest.

      CircleEvent predecessor = null;

      var node = _circleEvents.Root;

      while (node != null)
      {
        if (circleEvent.Y < node.Y || (circleEvent.Y == node.Y && circleEvent.X <= node.X))
        {
          if (node.LeftNode != null)
            node = node.LeftNode;
          else
          {
            predecessor = node.PreviousNode;

            break;
          }
        }
        else
        {
          if (node.RightNode != null)
            node = node.RightNode;
          else
          {
            predecessor = node;

            break;
          }
        }
      }

      if (predecessor != null)
        _circleEvents.InsertSuccessor(predecessor, circleEvent);
      else
        _firstCircleEvent = circleEvent;
    }
    #endregion

    #region ClipEdge
    private bool ClipEdge(Edge edge)
    {
      // Line-clipping code taken from: Liang-Barsky function by Daniel White - http://www.skytopia.com/project/articles/compsci/clipping.html.
      // A bit modified to minimize code paths.

      var ax = edge.VertexA.X;
      var ay = edge.VertexA.Y;
      var dx = edge.VertexB.X - ax;
      var dy = edge.VertexB.Y - ay;
      var t0 = 0.0;
      var t1 = 1.0;

      // Left.

      var q = ax - _box.Left;

      if (dx == 0.0 && q < 0.0)
        return false;

      var r = -q / dx;

      if (dx < 0.0)
      {
        if (r < t0)
          return false;
        else
          if (r < t1)
          t1 = r;
      }
      else
        if (dx > 0.0)
      {
        if (r > t1)
          return false;
        else
          if (r > t0)
          t0 = r;
      }

      // Right.

      q = _box.Right - ax;

      if (dx == 0.0 && q < 0.0)
        return false;

      r = q / dx;

      if (dx < 0.0)
      {
        if (r > t1)
          return false;
        else
          if (r > t0)
          t0 = r;
      }
      else
        if (dx > 0.0)
      {
        if (r < t0)
          return false;
        else
          if (r < t1)
          t1 = r;
      }

      // Top.

      q = ay - _box.Top;

      if (dy == 0.0 && q < 0.0)
        return false;

      r = -q / dy;

      if (dy < 0.0)
      {
        if (r < t0)
          return false;
        else
          if (r < t1)
          t1 = r;
      }
      else
        if (dy > 0.0)
      {
        if (r > t1)
          return false;
        else
          if (r > t0)
          t0 = r;
      }

      // Bottom.

      q = _box.Bottom - ay;

      if (dy == 0.0 && q < 0.0)
        return false;

      r = q / dy;

      if (dy < 0.0)
      {
        if (r > t1)
          return false;
        else
          if (r > t0)
          t0 = r;
      }
      else
        if (dy > 0.0)
      {
        if (r < t0)
          return false;
        else
          if (r < t1)
          t1 = r;
      }

      // If we reach this point, Voronoi edge is within bbox.

      // If t0 > 0, va needs to change.
      // We need to create a new vertex rather than modifying the existing one, since the existing one is likely shared with at least another edge.
      if (t0 > 0.0)
        edge.VertexA = new Vertex(ax + t0 * dx, ay + t0 * dy);

      // If t1 < 1, vb needs to change.
      // We need to create a new vertex rather than modifying the existing one, since the existing one is likely shared with at least another edge.
      if (t1 < 1.0)
        edge.VertexB = new Vertex(ax + t1 * dx, ay + t1 * dy);

      return true;
    }
    #endregion

    #region ClipEdges
    private void ClipEdges()
    {
      // Connect/cut edges at bounding box.
      // Connect all dangling edges to bounding box or get rid of them if it can't be done.

      for (var i = _edges.Count - 1; i >= 0; i--)
      {
        var edge = _edges[i];

        // Edge is removed if: it is wholly outside the bounding box or it is actually a point rather than a line.
        if (!ConnectEdge(edge) || !ClipEdge(edge) || (Eq(edge.VertexA.X, edge.VertexB.X) && Eq(edge.VertexA.Y, edge.VertexB.Y)))
        {
          edge.VertexA = edge.VertexB = null;

          _edges.RemoveAt(i);
        }
      }
    }
    #endregion

    #region CloseCells
    private void CloseCells()
    {
      // Close the cells.
      // The cells are bound by the supplied bounding box.
      // Each cell refers to its associated cell, and a list of halfedges ordered counterclockwise.

      // Prune, order halfedges, then add missing ones required to close cells.

      for (var i = _cells!.Length - 1; i >= 0; i--)
      {
        var cell = _cells[i];

        // Trim non fully-defined halfedges and sort them counterclockwise.
        if (cell.Prepare() == 0)
          continue;

        // Close open cells.

        // Step 1: find first 'unclosed' point, if any.
        // An 'unclosed' point will be the end point of a halfedge which does not match the start point of the following halfedge.

        var halfedges = cell.Halfedges;
        var nHalfedges = halfedges.Count;

        // Special case: only one cell, in which case, the viewport is the cell.
        // ...

        // All other cases.

        var left = 0;

        while (left < nHalfedges)
        {
          var right = (left + 1) % nHalfedges;

          var start = halfedges[right].Start;
          var end = halfedges[left].End;

          // If end point is not equal to start point, we need to add the missing halfedge(s) to close the cell.
          if (!Eq(end.X, start.X) || !Eq(end.Y, start.Y))
          {
            // If we reach this point, cell needs to be closed by walking counterclockwise along the bounding box until it connects to next halfedge in the list.

            var v = Eq(end.X, _box.Left) && Le(end.Y, _box.Bottom) ? new Vertex(_box.Left, Eq(start.X, _box.Left) ? start.Y : _box.Bottom) : // Walk downward along left side.
                    Eq(end.Y, _box.Bottom) && Le(end.X, _box.Right) ? new Vertex(Eq(start.Y, _box.Bottom) ? start.X : _box.Right, _box.Bottom) : // Walk rightward along bottom side.
                    Eq(end.X, _box.Right) && Gr(end.Y, _box.Top) ? new Vertex(_box.Right, Eq(start.X, _box.Right) ? start.Y : _box.Top) : // Walk upward along right side.
                    Eq(end.Y, _box.Top) && Gr(end.X, _box.Left) ? new Vertex(Eq(start.Y, _box.Top) ? start.X : _box.Left, _box.Top) : // Walk leftward along top side.
                    new Vertex(double.NaN, double.NaN); // Never should happen.

            var edge = CreateBorderEdge(cell, end, v);

            halfedges.Insert(left + 1, new Halfedge(edge, cell, null!));

            nHalfedges = halfedges.Count;
          }

          left++;
        }
      }
    }
    #endregion

    #region CreateBorderEdge
    private Edge CreateBorderEdge(Cell leftCell, Vertex vertexA, Vertex vertexB)
    {
      var edge = new Edge(leftCell, null!) { VertexA = vertexA, VertexB = vertexB };

      _edges.Add(edge);

      return edge;
    }
    #endregion

    #region CreateEdge
    private Edge CreateEdge(Cell leftCell, Cell rightCell, Vertex vertexA, Vertex vertexB)
    {
      // This creates and adds an edge to internal collection, and also creates two halfedges which are added to each cell's counterclockwise array of halfedges.

      var edge = new Edge(leftCell, rightCell);

      _edges.Add(edge);

      if (vertexA != null)
        SetEdgeStart(edge, leftCell, rightCell, vertexA);

      if (vertexB != null)
        SetEdgeEnd(edge, leftCell, rightCell, vertexB);

      leftCell.Halfedges.Add(new Halfedge(edge, leftCell, rightCell));
      rightCell.Halfedges.Add(new Halfedge(edge, rightCell, leftCell));

      return edge;
    }
    #endregion

    #region ConnectEdge
    private bool ConnectEdge(Edge edge)
    {
      // Connect dangling edges (not if a cursory test tells us it is not going to be visible).
      // Return value: False - the dangling endpoint couldn't be connected, True - the dangling endpoint could be connected.

      var vb = edge.VertexB;

      if (vb != null) // Skip if end point already connected.
        return true;

      var va = edge.VertexA;
      var lCell = edge.LeftCell;
      var rCell = edge.RightCell;
      var lx = lCell.X;
      var ly = lCell.Y;
      var rx = rCell.X;
      var ry = rCell.Y;
      var fx = (lx + rx) / 2.0;
      var fy = (ly + ry) / 2.0;
      double fm = double.NaN;
      double fb = double.NaN;

      // Get the line equation of the bisector if line is not vertical.
      if (ry != ly)
      {
        fm = (lx - rx) / (ry - ly);
        fb = fy - fm * fx;
      }

      // Remember, direction of line (relative to left cell):
      // upward: left.x < right.x
      // downward: left.x > right.x
      // horizontal: left.x == right.x
      // upward: left.x < right.x
      // rightward: left.y < right.y
      // leftward: left.y > right.y
      // vertical: left.y == right.y

      // Depending on the direction, find the best side of the bounding box to use to determine a reasonable start point.

      if (double.IsNaN(fm)) // Special case: vertical line
      {
        if (fx < _box.Left || fx >= _box.Right) // Doesn't intersect with viewport.
          return false;

        if (lx > rx) // Downward.
        {
          if (va == null)
            va = new Vertex(fx, _box.Top);
          else
            if (va.Y >= _box.Bottom)
            return false;

          vb = new Vertex(fx, _box.Bottom);
        }
        else // Upward.
        {
          if (va == null)
            va = new Vertex(fx, _box.Bottom);
          else
            if (va.Y < _box.Top)
            return false;

          vb = new Vertex(fx, _box.Top);
        }
      }
      else // Closer to vertical than horizontal, connect start point to the top or bottom side of the bounding box.
        if (fm < -1.0 || fm > 1.0)
      {
        if (lx > rx) // Downward.
        {
          if (va == null)
            va = new Vertex((_box.Top - fb) / fm, _box.Top);
          else
            if (va.Y >= _box.Bottom)
            return false;

          vb = new Vertex((_box.Bottom - fb) / fm, _box.Bottom);
        }
        else // Upward.
        {
          if (va == null)
            va = new Vertex((_box.Bottom - fb) / fm, _box.Bottom);
          else
            if (va.Y < _box.Top)
            return false;

          vb = new Vertex((_box.Top - fb) / fm, _box.Top);
        }
      }
      else // Closer to horizontal than vertical, connect start point to the left or right side of the bounding box.
      {
        if (ly < ry) // Rightward.
        {
          if (va == null)
            va = new Vertex(_box.Left, fm * _box.Left + fb);
          else
            if (va.X >= _box.Right)
            return false;

          vb = new Vertex(_box.Right, fm * _box.Right + fb);
        }
        else // Leftward.
        {
          if (va == null)
            va = new Vertex(_box.Right, fm * _box.Right + fb);
          else
            if (va.X < _box.Left)
            return false;

          vb = new Vertex(_box.Left, fm * _box.Left + fb);
        }
      }

      edge.VertexA = va;
      edge.VertexB = vb;

      return true;
    }
    #endregion

    #region DetachBeachsection
    private void DetachBeachsection(BeachSection beachSection)
    {
      DetachCircleEvent(beachSection); // Detach potentially attached circle event.

      _beachLine.RemoveNode(beachSection); // Remove from RB-tree.
    }
    #endregion

    #region DetachCircleEvent
    private void DetachCircleEvent(BeachSection arc)
    {
      if (arc.CircleEvent != null)
      {
        if (arc.CircleEvent.PreviousNode == null)
          _firstCircleEvent = arc.CircleEvent.NextNode;

        _circleEvents.RemoveNode(arc.CircleEvent); // Remove from RB-tree.

        arc.CircleEvent = null;
      }
    }
    #endregion

    #region Eq
    private static bool Eq(double a, double b)
    {
      return Math.Abs(a - b) < EPSILON;
    }
    #endregion

    #region Get
    /// <summary>
    /// Возвращает список полигонов, соответствующих входным точкам.
    /// Полигоном в данном случае является список точек, окружающих входную точку и упорядоченных против часовой стрелки.
    /// </summary>
    /// <param name="points">Список точек.</param>
    /// <returns>Список полигонов.</returns>
    public Point[][] Get([NotNull] Point[] points)
    {
      try
      {
        if (points.Length == 0)
          return new Point[0][];
        else
          if (points.Length == 1)
        {
          if (points[0].X.InRange(_box.Left, _box.Right) && points[0].Y.InRange(_box.Bottom, _box.Top))
            return new Point[1][] { new Point[] { new Point(_box.Left, _box.Bottom), new Point(_box.Right, _box.Bottom), new Point(_box.Right, _box.Top), new Point(_box.Left, _box.Top) } };
          else
            return new Point[1][];
        }

        var precisionMuliplier = Math.Pow(10, _precision);

        var top = _box.Top;

        _box.Left = Math.Round(_box.Left * precisionMuliplier);
        _box.Right = Math.Round(_box.Right * precisionMuliplier);
        _box.Top = Math.Round(_box.Bottom * precisionMuliplier); // Required swapping to
        _box.Bottom = Math.Round(top * precisionMuliplier);      // algorithms work correctly.

        _cells = new Cell[points.Length];

        for (var i = 0; i < points.Length; i++)
          _cells[i] = new Cell(new Point(Math.Round(points[i].X * precisionMuliplier), Math.Round(points[i].Y * precisionMuliplier)));

        var cellEvents = new List<Cell>(_cells);

        cellEvents.Sort((a, b) =>
       {
         var r = b.Y - a.Y;

         if (r == 0)
           r = b.X - a.X;

         return r < 0.0 ? -1 :
                r > 0.0 ? 1 :
                0;
       });

        // To avoid duplicate cells.

        Cell prevCell = null;

        var cell = cellEvents.Count > 0 ? cellEvents[cellEvents.Count - 1] : null;

        for (var i = cellEvents.Count - 2; ;)
        {
          // We need to figure whether we handle a cell or circle event for this we find out if there is a cell event and it is 'earlier' than the circle event.
          var circle = _firstCircleEvent;

          if (cell != null && (circle == null || cell.Y < circle.Y || (cell.Y == circle.Y && cell.X < circle.X))) // Add beach section.
          {
            if (prevCell == null || cell.X != prevCell.X || cell.Y != prevCell.Y) // Only if cell is not a duplicate.
            {
              // Create a beachsection for that cell.
              AddBeachsection(cell);

              prevCell = cell;
            }

            cell = i >= 0 ? cellEvents[i--] : null;
          }
          else
            if (circle != null) // remove beach section
            RemoveBeachsection(circle.Arc);
          else
            break;
        }

        // Wrapping-up: connect dangling edges to bounding box; cut edges as per bounding box; discard edges completely outside bounding box; discard edges which are point-like.
        ClipEdges();

        // Add missing edges in order to close opened cells.
        CloseCells();

        for (var i = cellEvents.Count - 2; i >= 0; i--)
          if (cellEvents[i].Halfedges.Count == 0)
            cellEvents[i].Halfedges.AddRange(cellEvents[i + 1].Halfedges);

        var polygons = new Point[_cells.Length][];

        for (var i = 0; i < _cells.Length; i++)
        {
          var halfedges = _cells[i].Halfedges;

          var polygon = polygons[i] = new Point[halfedges.Count];

          for (var j = 0; j < halfedges.Count; j++)
          {
            var vertex = halfedges[j].Start;

            var index = halfedges.Count - 1 - j;

            polygon[index].X = Math.Round(vertex.X / precisionMuliplier, _precision);
            polygon[index].Y = Math.Round(vertex.Y / precisionMuliplier, _precision);
          }
        }

        return polygons;
      }
      finally
      {
        _beachLine.Root = null;

        _circleEvents.Root = _firstCircleEvent = null;

        _edges.Clear();

        _cells = null;
      }
    }
    #endregion

    #region GetLeftBreakPoint
    private double GetLeftBreakPoint(BeachSection arc, double directrix)
    {
      // Calculate the left break point of a particular beach section, given a particular sweep line.

      var rfocx = arc.Cell.X;
      var rfocy = arc.Cell.Y;
      var pby2 = rfocy - directrix;

      // Parabola in degenerate case where focus is on directrix.
      if (pby2 == 0.0)
        return rfocx;

      var lArc = arc.PreviousNode;

      if (lArc == null)
        return double.NegativeInfinity;

      var lfocx = lArc.Cell.X;
      var lfocy = lArc.Cell.Y;
      var plby2 = lfocy - directrix;

      // Parabola in degenerate case where focus is on directrix.
      if (plby2 == 0.0)
        return lfocx;

      var hl = lfocx - rfocx;
      var aby2 = 1.0 / pby2 - 1.0 / plby2;
      var b = hl / plby2;

      if (aby2 != 0.0)
        return (-b + Math.Sqrt(b * b - 2.0 * aby2 * (hl * hl / (-2.0 * plby2) - lfocy + plby2 / 2.0 + rfocy - pby2 / 2.0))) / aby2 + rfocx;

      // Both parabolas have same distance to directrix, thus break point is midway.
      return (rfocx + lfocx) / 2.0;
    }
    #endregion

    #region GetRightBreakPoint
    private double GetRightBreakPoint(BeachSection arc, double directrix)
    {
      // Calculate the right break point of a particular beach section, given a particular directrix.

      if (arc.NextNode != null)
        return GetLeftBreakPoint(arc.NextNode, directrix);

      return arc.Cell.Y == directrix ? arc.Cell.X : double.PositiveInfinity;
    }
    #endregion

    #region Gr
    private bool Gr(double a, double b)
    {
      return a - b > EPSILON;
    }
    #endregion

    #region GrOrEq
    private bool GrOrEq(double a, double b)
    {
      return b - a < EPSILON;
    }
    #endregion

    #region Le
    private bool Le(double a, double b)
    {
      return b - a > EPSILON;
    }
    #endregion

    #region LeOrEq
    private bool LeOrEq(double a, double b)
    {
      return a - b < EPSILON;
    }
    #endregion

    #region RemoveBeachsection
    private void RemoveBeachsection(BeachSection beachSection)
    {
      var circle = beachSection.CircleEvent;

      var x = circle.X;
      var y = circle.YCenter;

      var vertex = new Vertex(x, y);

      var previous = beachSection.PreviousNode;
      var next = beachSection.NextNode;

      var disappearingTransitions = new List<BeachSection>(new[] { beachSection });

      // Remove collapsed beachsection from beachline.
      DetachBeachsection(beachSection);

      // There could be more than one empty arc at the deletion point, this happens when more than two edges are linked by the same vertex,
      // so we will collect all those edges by looking up both sides of the deletion point. Btw, there is *always* a predecessor/successor to any collapsed
      // beach section, it's just impossible to have a collapsing first/last beach sections on the beachline, since they obviously are unconstrained on their left/right side.

      // Look left.

      var lArc = previous;

      while (lArc.CircleEvent != null && Eq(x, lArc.CircleEvent.X) && Eq(y, lArc.CircleEvent.YCenter))
      {
        previous = lArc.PreviousNode;

        disappearingTransitions.Insert(0, lArc);

        DetachBeachsection(lArc);

        lArc = previous;
      }

      // Even though it is not disappearing, I will also add the beach section immediately to the left of the left-most collapsed beach section, for
      // convenience, since we need to refer to it later as this beach section is the 'left' cell of an edge for which a start point is set.

      disappearingTransitions.Insert(0, lArc);

      DetachCircleEvent(lArc);

      // Look right.

      var rArc = next;

      while (rArc.CircleEvent != null && Eq(x, rArc.CircleEvent.X) && Eq(y, rArc.CircleEvent.YCenter))
      {
        next = rArc.NextNode;

        disappearingTransitions.Add(rArc);

        DetachBeachsection(rArc);

        rArc = next;
      }

      // We also have to add the beach section immediately to the right of the right-most collapsed beach section, since there is also a disappearing
      // transition representing an edge's start point on its left.

      disappearingTransitions.Add(rArc);

      DetachCircleEvent(rArc);

      // Walk through all the disappearing transitions between beach sections and set the start point of their (implied) edge.

      var nArcs = disappearingTransitions.Count;

      for (var i = 1; i < nArcs; i++)
      {
        rArc = disappearingTransitions[i];
        lArc = disappearingTransitions[i - 1];

        SetEdgeStart(rArc.Edge, lArc.Cell, rArc.Cell, vertex);
      }

      // Create a new edge as we have now a new transition between two beach sections which were previously not adjacent.
      // Since this edge appears as a new vertex is defined, the vertex actually define an end point of the edge (relative to the cell on the left).

      lArc = disappearingTransitions[0];
      rArc = disappearingTransitions[nArcs - 1];

      rArc.Edge = CreateEdge(lArc.Cell, rArc.Cell, null, vertex);

      // Create circle events if any for beach sections left in the beachline adjacent to collapsed sections.

      AttachCircleEvent(lArc);
      AttachCircleEvent(rArc);
    }
    #endregion

    #region SetEdgeEnd
    private void SetEdgeEnd(Edge edge, Cell leftCell, Cell rightCell, Vertex vertex)
    {
      SetEdgeStart(edge, rightCell, leftCell, vertex);
    }
    #endregion

    #region SetEdgeStart
    private void SetEdgeStart(Edge edge, Cell leftCell, Cell rightCell, Vertex vertex)
    {
      if (edge.VertexA == null && edge.VertexB == null)
      {
        edge.VertexA = vertex;

        edge.LeftCell = leftCell;
        edge.RightCell = rightCell;
      }
      else
        if (edge.LeftCell == rightCell)
        edge.VertexB = vertex;
      else
        edge.VertexA = vertex;
    }
    #endregion

    /// <summary>
    /// Прямоугольник, ограничивающий построение диаграммы.
    /// </summary>
    public sealed class Box
    {
      #region .Fields
      public double Left;
      public double Right;
      public double Top;
      public double Bottom;
      #endregion
    }

    private sealed class Cell
    {
      #region .Fields
      public readonly Point Point;

      public readonly List<Halfedge> Halfedges = new List<Halfedge>();
      #endregion

      #region .Properties
      public double X
      {
        get { return Point.X; }
      }

      public double Y
      {
        get { return Point.Y; }
      }
      #endregion

      #region .Ctor
      public Cell(Point point)
      {
        Point = point;
      }
      #endregion

      #region Prepare
      public int Prepare()
      {
        // Get rid of unused halfedges. Keep it simple, no point here in trying to be fancy: dangling edges are a typically a minority.
        for (var i = Halfedges.Count - 1; i >= 0; i--)
        {
          var edge = Halfedges[i].Edge;

          if (edge.VertexB == null || edge.VertexA == null)
            Halfedges.RemoveAt(i);
        }

        Halfedges.Sort((a, b) =>
       {
         var angle = b.Angle - a.Angle;

         return angle < 0.0 ? -1 :
                angle > 0.0 ? 1 :
                0;
       });

        return Halfedges.Count;
      }
      #endregion

      #region ToString
      public override string ToString()
      {
        return string.Format("({0}; {1})", X, Y);
      }
      #endregion
    }

    private sealed class Vertex
    {
      #region .Fields
      public readonly double X;
      public readonly double Y;
      #endregion

      #region .Ctor
      public Vertex(double x, double y)
      {
        X = x;
        Y = y;
      }
      #endregion

      #region ToString
      public override string ToString()
      {
        return string.Format("({0}; {1})", X, Y);
      }
      #endregion
    }

    private sealed class Edge
    {
      #region .Fields
      public Cell LeftCell;
      public Cell RightCell;

      public Vertex VertexA;
      public Vertex VertexB;
      #endregion

      #region .Ctor
      public Edge(Cell leftCell, Cell rightCell)
      {
        LeftCell = leftCell;
        RightCell = rightCell;
      }
      #endregion

      #region ToString
      public override string ToString()
      {
        return string.Format("{0}-{1}, Left{2}, Right{3}", VertexA, VertexB, LeftCell, RightCell);
      }
      #endregion
    }

    private sealed class Halfedge
    {
      #region .Fields
      public readonly Cell Cell;

      public readonly Edge Edge;

      public readonly double Angle;
      #endregion

      #region .Properties
      public Vertex Start
      {
        get { return Edge.LeftCell == Cell ? Edge.VertexA : Edge.VertexB; }
      }

      public Vertex End
      {
        get { return Edge.LeftCell == Cell ? Edge.VertexB : Edge.VertexA; }
      }
      #endregion

      #region .Ctor
      public Halfedge(Edge edge, Cell leftCell, Cell rightCell)
      {
        Cell = leftCell;

        Edge = edge;

        // 'angle' is a value to be used for properly sorting the halfsegments counterclockwise. By convention, we will use the angle of the line defined by the 'cell to the left' to the 'cell to the right'.
        // However, border edges have no 'cell to the right': thus we use the angle of line perpendicular to the halfsegment (the edge should have both end points defined in such case).
        if (rightCell != null)
          Angle = Math.Atan2(rightCell.Y - leftCell.Y, rightCell.X - leftCell.X);
        else
        {
          var va = edge.VertexA;
          var vb = edge.VertexB;

          // Used to call getStartpoint()/getEndpoint(), but for performance purpose, these are expanded in place here.
          Angle = edge.LeftCell == leftCell ? Math.Atan2(vb.X - va.X, va.Y - vb.Y) : Math.Atan2(va.X - vb.X, vb.Y - va.Y);
        }
      }
      #endregion

      #region ToString
      public override string ToString()
      {
        return string.Format("{0}-{1}, Cell{2}", Edge.VertexA, Edge.VertexB, Cell);
      }
      #endregion
    }

    private sealed class BeachSection : RedBlackTree<BeachSection>.Node
    {
      #region .Fields
      public readonly Cell Cell;

      public Edge Edge;

      public CircleEvent CircleEvent;
      #endregion

      #region .Ctor
      public BeachSection(Cell cell)
      {
        Cell = cell;
      }
      #endregion

      #region ToString
      public override string ToString()
      {
        return CircleEvent == null ? "Cell" + Cell : string.Format("Cell{0}, Circle({1}; {2}[{3}])", Cell, CircleEvent.X, CircleEvent.Y, CircleEvent.YCenter);
      }
      #endregion
    }

    private sealed class CircleEvent : RedBlackTree<CircleEvent>.Node
    {
      #region .Fields
      public readonly BeachSection Arc;
      public readonly Cell Cell;
      public readonly double X;
      public readonly double Y;
      public readonly double YCenter;
      #endregion

      #region .Ctor
      public CircleEvent(BeachSection arc, Cell cell, double x, double y, double yCenter)
      {
        Arc = arc;
        Cell = cell;
        X = x;
        Y = y;
        YCenter = yCenter;
      }
      #endregion

      #region ToString
      public override string ToString()
      {
        return string.Format("Circle({0}; {1}[{2}]), Cell({3})", X, Y, YCenter, Cell);
      }
      #endregion
    }

    private class RedBlackTree<T>
      where T : RedBlackTree<T>.Node
    {
      #region .Fields
      public T Root;
      #endregion

      #region GetFirst
      public T GetFirst(T node)
      {
        while (node.LeftNode != null)
          node = node.LeftNode;

        return node;
      }
      #endregion

      #region GetLast
      public T GetLast(T node)
      {
        while (node.RightNode != null)
          node = node.RightNode;

        return node;
      }
      #endregion

      #region InsertSuccessor
      public void InsertSuccessor(T node, T successor)
      {
        T parent;

        if (node != null)
        {
          successor.PreviousNode = node;
          successor.NextNode = node.NextNode;

          if (node.NextNode != null)
            node.NextNode.PreviousNode = successor;

          node.NextNode = successor;

          if (node.RightNode != null)
          {
            // In-place expansion of node.RightNode.GetFirst().
            node = node.RightNode;

            while (node.LeftNode != null)
              node = node.LeftNode;

            node.LeftNode = successor;
          }
          else
            node.RightNode = successor;

          parent = node;
        }
        else // If node is null, successor must be inserted to the left-most part of the tree.
          if (Root != null)
        {
          node = GetFirst(Root);

          successor.PreviousNode = null;
          successor.NextNode = node;

          node.PreviousNode = successor;
          node.LeftNode = successor;

          parent = node;
        }
        else
        {
          successor.PreviousNode = successor.NextNode = null;

          Root = successor;

          parent = null;
        }

        successor.LeftNode = successor.RightNode = null;
        successor.ParentNode = parent;
        successor.IsRed = true;

        // Fixup the modified tree by recoloring nodes and performing rotations (2 at most) hence the red-black tree properties are preserved.

        node = successor;

        while (parent != null && parent.IsRed)
        {
          var grandpa = parent.ParentNode;

          if (parent == grandpa.LeftNode)
          {
            var uncle = grandpa.RightNode;

            if (uncle != null && uncle.IsRed)
            {
              parent.IsRed = uncle.IsRed = false;
              grandpa.IsRed = true;
              node = grandpa;
            }
            else
            {
              if (node == parent.RightNode)
              {
                RotateLeft(parent);

                node = parent;
                parent = node.ParentNode;
              }

              parent.IsRed = false;
              grandpa.IsRed = true;

              RotateRight(grandpa);
            }
          }
          else
          {
            var uncle = grandpa.LeftNode;

            if (uncle != null && uncle.IsRed)
            {
              parent.IsRed = uncle.IsRed = false;
              grandpa.IsRed = true;
              node = grandpa;
            }
            else
            {
              if (node == parent.LeftNode)
              {
                RotateRight(parent);

                node = parent;
                parent = node.ParentNode;
              }

              parent.IsRed = false;
              grandpa.IsRed = true;

              RotateLeft(grandpa);
            }
          }

          parent = node.ParentNode;
        }

        Root.IsRed = false;
      }
      #endregion

      #region RemoveNode
      public void RemoveNode(T node)
      {
        if (node.NextNode != null)
          node.NextNode.PreviousNode = node.PreviousNode;

        if (node.PreviousNode != null)
          node.PreviousNode.NextNode = node.NextNode;

        node.NextNode = node.PreviousNode = null;

        var parent = node.ParentNode;
        var left = node.LeftNode;
        var right = node.RightNode;

        var next = left == null ? right :
                   right == null ? left :
                   GetFirst(right);

        if (parent != null)
        {
          if (parent.LeftNode == node)
            parent.LeftNode = next;
          else
            parent.RightNode = next;
        }
        else
          Root = next;

        // Enforce red-black rules.

        bool isRed;

        if (left != null && right != null)
        {
          isRed = next.IsRed;

          next.IsRed = node.IsRed;
          next.LeftNode = left;
          left.ParentNode = next;

          if (next != right)
          {
            parent = next.ParentNode;
            next.ParentNode = node.ParentNode;
            node = next.RightNode;
            parent.LeftNode = node;
            next.RightNode = right;
            right.ParentNode = next;
          }
          else
          {
            next.ParentNode = parent;
            parent = next;
            node = next.RightNode;
          }
        }
        else
        {
          isRed = node.IsRed;

          node = next;
        }

        // 'node' is now the sole successor's child and 'parent' its new parent (since the successor can have been moved).
        if (node != null)
          node.ParentNode = parent;

        // The 'easy' cases.
        if (isRed)
          return;

        if (node != null && node.IsRed)
        {
          node.IsRed = false;

          return;
        }

        // The other cases.
        do
        {
          if (node == Root)
            break;

          T sibling;

          if (node == parent.LeftNode)
          {
            sibling = parent.RightNode;

            if (sibling.IsRed)
            {
              sibling.IsRed = false;
              parent.IsRed = true;

              RotateLeft(parent);

              sibling = parent.RightNode;
            }

            if ((sibling.LeftNode != null && sibling.LeftNode.IsRed) || (sibling.RightNode != null && sibling.RightNode.IsRed))
            {
              if (sibling.RightNode == null || !sibling.RightNode.IsRed)
              {
                sibling.LeftNode.IsRed = false;
                sibling.IsRed = true;

                RotateRight(sibling);

                sibling = parent.RightNode;
              }

              sibling.IsRed = parent.IsRed;
              parent.IsRed = sibling.RightNode.IsRed = false;

              RotateLeft(parent);

              node = Root;

              break;
            }
          }
          else
          {
            sibling = parent.LeftNode;

            if (sibling.IsRed)
            {
              sibling.IsRed = false;
              parent.IsRed = true;

              RotateRight(parent);

              sibling = parent.LeftNode;
            }

            if ((sibling.LeftNode != null && sibling.LeftNode.IsRed) || (sibling.RightNode != null && sibling.RightNode.IsRed))
            {
              if (sibling.LeftNode == null || !sibling.LeftNode.IsRed)
              {
                sibling.RightNode.IsRed = false;
                sibling.IsRed = true;

                RotateLeft(sibling);

                sibling = parent.LeftNode;
              }

              sibling.IsRed = parent.IsRed;
              parent.IsRed = sibling.LeftNode.IsRed = false;

              RotateRight(parent);

              node = Root;

              break;
            }
          }

          sibling.IsRed = true;

          node = parent;
          parent = parent.ParentNode;
        } while (!node.IsRed);

        if (node != null)
          node.IsRed = false;
      }
      #endregion

      #region RotateLeft
      public void RotateLeft(T node)
      {
        var p = node;
        var q = node.RightNode; // Can't be null.
        var parent = p.ParentNode;

        if (parent != null)
        {
          if (parent.LeftNode == p)
            parent.LeftNode = q;
          else
            parent.RightNode = q;
        }
        else
          Root = q;

        q.ParentNode = parent;
        p.ParentNode = q;
        p.RightNode = q.LeftNode;

        if (p.RightNode != null)
          p.RightNode.ParentNode = p;

        q.LeftNode = p;
      }
      #endregion

      #region RotateRight
      public void RotateRight(T node)
      {
        var p = node;
        var q = node.LeftNode; // Can't be null.
        var parent = p.ParentNode;

        if (parent != null)
        {
          if (parent.LeftNode == p)
            parent.LeftNode = q;
          else
            parent.RightNode = q;
        }
        else
          Root = q;

        q.ParentNode = parent;
        p.ParentNode = q;
        p.LeftNode = q.RightNode;

        if (p.LeftNode != null)
          p.LeftNode.ParentNode = p;

        q.RightNode = p;
      }
      #endregion

      public class Node
      {
        #region .Fields
        public bool IsRed;
        public T ParentNode;
        public T LeftNode;
        public T RightNode;
        public T NextNode;
        public T PreviousNode;
        #endregion
      }
    }
  }
}
