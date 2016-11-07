using UnityEngine;
using System;
using System.Collections.Generic;
using OpenCvSharp;
using OpenCvSharp.CPlusPlus;
using System.Runtime.InteropServices;

namespace HandRecoginition {
    class PointRecognition {
        const int R = 37;
        const int C = 65;
        const int POINT_DIST = 4;
        const int CROSS_SIZE = 2;
        const int MAX_EDGE_LEN = 10;
        const float LINE_K = 0.3f;
        const int BLACK_THRESHOLD = 20;
        int[] DIR_X = {0, 1, 0, -1};
        int[] DIR_Y = {-1, 0, 1, 0};

        const int CENTRE_Y = 165;
        const float BLOCK_Y = 10.81f;
        const float Z_D_RATIO = 0.01f;

        int[,] mat, sum;

        /*public int[,] getMatFromImage() {
            Mat src = new Mat("input.jpg", LoadMode.GrayScale);

            int H = src.Height;
            int W = src.Width;
            int[,] mat = new int[W, H];
            IntPtr ptr = src.Data;
            for (int x = 0; x < src.Width; x++) {
                for (int y = 0; y < src.Height; y++) {
                    mat[x, y] = Marshal.ReadByte(ptr, src.Width * y + x);
                }
            }
            
            return mat;
        }*/

        private Point[] calnPoints() {
            List<Point> points = new List<Point>();

            int W = mat.GetLength(0);
            int H = mat.GetLength(1);
            int[,] rowSum = new int[W, H];
            int[,] colSum = new int[W, H];
            sum = new int[W, H];
            for (int x = 0; x < W; x++) {
                for (int y = 0; y < H; y++) {
                    if (mat[x, y] > BLACK_THRESHOLD) {
                        colSum[x, y] = rowSum[x, y] = mat[x, y];
                        if (x - 1 >= 0) {
                            rowSum[x, y] += rowSum[x - 1, y];
                        }
                        if (y - 1 >= 0) {
                            colSum[x, y] += colSum[x, y - 1];
                        }
                    }
                }
            }
            for (int x = CROSS_SIZE + 1; x + CROSS_SIZE + 1 < W; x++) {
                for (int y = CROSS_SIZE + 1; y + CROSS_SIZE + 1 < H; y++) {
                    if (mat[x, y] > BLACK_THRESHOLD) {
                        sum[x, y] = -mat[x, y];
                        sum[x, y] += rowSum[x + CROSS_SIZE, y] - rowSum[x - CROSS_SIZE - 1, y];
                        sum[x, y] += colSum[x, y + CROSS_SIZE] - colSum[x, y - CROSS_SIZE - 1];
                    }
                }
            }

            for (int x = CROSS_SIZE + 2; x + CROSS_SIZE + 2 < W; x++) {
                for (int y = CROSS_SIZE + 2; y + CROSS_SIZE + 2 < H; y++) {
                    if (mat[x, y] > BLACK_THRESHOLD && sum[x, y] > sum[x - 1, y] && sum[x, y] > sum[x + 1, y] && sum[x, y] > sum[x, y - 1] && sum[x, y] > sum[x, y + 1]) {
                        bool check = true;
                        for (int i = points.Count - 1; i >= 0; i--) {
                            if (x - points[i].X > POINT_DIST) {
                                break;
                            }
                            if (Math.Abs(y - points[i].Y) <= POINT_DIST) {
                                if (sum[x, y] > sum[points[i].X, points[i].Y]) {
                                    points.Remove(points[i]);
                                } else {
                                    check = false;
                                    break;
                                }
                            }
                        }
                        if (check) {
                            points.Add(new Point(x, y));
                        }
                    }
                }
            }

            return points.ToArray();
        }

        private bool checkLine(int x0, int y0, int x1, int y1) {
            if (x0 == x1) {
                return true;
            }
            double k = Math.Abs((y1 - y0) / (x1 - x0));
            return k <= LINE_K || k >= 1 / LINE_K;
        }

        private int[,] calnEdges(Point[] points) {
            int n = points.Length;
            int[,] edges = new int[n, 4];
            
            for (int i = 0; i < n; i++) {
                double[] minDist = new double[4];
                for (int d = 0; d < 4; d++) {
                    minDist[d] = MAX_EDGE_LEN;
                    edges[i, d] = -1;
                }
                for (int j = i - 1; j >= 0; j--) {
                    if (points[i].X - points[j].X > MAX_EDGE_LEN) {
                        break;
                    }
                    int d = -1;
                    if (points[i].X - points[j].X < Math.Abs(points[i].Y - points[j].Y)) {
                        if (points[j].Y < points[i].Y) {
                            d = 0;
                        } else {
                            d = 2;
                        }
                    } else {
                        d = 3;
                    }
                    double dist = points[i].DistanceTo(points[j]);
                    if (dist < minDist[d]) {
                        if (checkLine(points[i].X, points[i].Y, points[j].X, points[j].Y)) {
                            minDist[d] = dist;
                            edges[i, d] = j;
                            edges[j, d ^ 2] = i;
                        }
                    }
                }
            }
            return edges;
        }

        private class EdgeInfo : IComparable {
            public int u, v;
            public int d;
            public double len;

            public EdgeInfo(int u, int v, int d, double len) {
                this.u = u;
                this.v = v;
                this.d = d;
                this.len = len;
            }

            public int CompareTo(object obj) {
                try {
                    EdgeInfo edge = obj as EdgeInfo;
                    if (this.len < edge.len) {
                        return -1;
                    } else {
                        return 1;
                    }
                } catch (Exception ex) {
                    throw new Exception(ex.Message);
                }
            }
        }

        private Point[] calnCoords(Point[] points, int[,] edges) {
            int n = points.Length;
            Point[] coords = new Point[n];
            int[] par = new int[n];
            List<int>[] member = new List<int>[n];

            for (int i = 0; i < n; i++) {
                par[i] = i;
                member[i] = new List<int>();
                member[i].Add(i);
            }
            List<EdgeInfo> order = new List<EdgeInfo>();

            for (int i = 0; i < n; i++) {
                for (int d = 0; d < 4; d++) {
                    if (edges[i, d] != -1) {
                        order.Add(new EdgeInfo(i, edges[i, d], d, points[i].DistanceTo(points[edges[i, d]])));
                    }
                }
            }
            order.Sort();

            for (int i = 0; i < order.Count; i++) {
                int u = order[i].u;
                int v = order[i].v;
                int d = order[i].d;
                if (par[u] != par[v]) {
                    if (member[par[u]].Count < member[par[v]].Count) {
                        int tmp = u;
                        u = v;
                        v = tmp;
                        d ^= 2;
                    }
                    Point shift = new Point(coords[u].X - coords[v].X + DIR_X[d], coords[u].Y - coords[v].Y + DIR_Y[d]);
                    int parV = par[v];
                    for (int j = 0; j < member[parV].Count; j++) {
                        int id = member[parV][j];
                        coords[id] += shift;
                        member[par[u]].Add(id);
                        par[id] = par[u];
                    }
                    member[parV].Clear();
                }
            }

            for (int i = 0; i < n; i++) {
                for (int d = 0; d < 4; d++) {
                    int v = edges[i, d];
                    if (v != -1) {
                        if (coords[i].X + DIR_X[d] != coords[v].X || coords[i].Y + DIR_Y[d] != coords[v].Y) {
                            edges[i, d] = -1;
                        }
                    }
                }
            }

            return coords;
        }

        private Point[] calibrateCoords(Point[] points, Point[] coords) {
            int W = mat.GetLength(0);
            int H = mat.GetLength(1);

            int centreX = 0, centreY = 0;
            double maxPower = 0;
            for (int i = 0; i < coords.Length; i++) {
                int x = points[i].X, y = points[i].Y;
                if (x - CROSS_SIZE >= 0 && x + CROSS_SIZE < W && y - CROSS_SIZE >= 0 && y + CROSS_SIZE < H) {
                    double power = 0;
                    for (int j = 0; j < CROSS_SIZE; j++) {
                        power += sum[x - j, y - j] + sum[x - j, y + j] + sum[x + j, y - j] + sum[x + j, y + j];
                    }
                    if (power > maxPower) {
                        maxPower = power;
                        centreX = coords[i].X;
                        centreY = coords[i].Y;
                    }
                }
            }
            
            for (int i = 0; i < coords.Length; i++) {
                coords[i] -= new Point(centreX, centreY);
            }
            return coords;
        }

        public int[] recognize(int[,] mat) {
            this.mat = mat;

            int W = mat.GetLength(0);
            int H = mat.GetLength(1);

            Point[] points = calnPoints();

            int[,] edges = calnEdges(points);
            Point[] coords = calnCoords(points, edges);
            coords = calibrateCoords(points, coords);

            int[] vec = new int[R * C * 2];
            for (int i = 0; i < vec.Length; i++) {
                vec[i] = -1;
            }
            for (int i = 0; i < points.Length; i++) {
                int r = coords[i].Y + R / 2;
                int c = coords[i].X + C / 2;
                if (0 <= r && r < R && 0 <= c && c < C) {
                    vec[(r * C + c) * 2] = points[i].X;
                    vec[(r * C + c) * 2 + 1] = points[i].Y;
                }
            }

            return vec;
        }
    }
}
