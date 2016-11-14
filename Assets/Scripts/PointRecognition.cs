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
        const int MIN_CENTRE_BLOCK = 100;
        const float CENTRE_X0 = 0.3f;
        const float CENTRE_X1 = 0.7f;
        const float CENTRE_Y0 = 0.0f;
        const float CENTRE_Y1 = 1.0f;
        Point[] DIR = {new Point(0, -1), new Point(1, 0), new Point(0, 1), new Point(-1, 0)};

        int W, H, X0, X1, Y0, Y1;
        int[,] mat, sum;

        public int[,] getMatFromImage() {
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
        }

        private void initialize() {
            W = mat.GetLength(0);
            H = mat.GetLength(1);

            X0 = W;
            X1 = 0;
            Y0 = H;
            Y1 = 0;
            for (int x = 0; x < W; x += 3) {
                for (int y = 0; y < H; y += 3) {
                    if (mat[x, y] > BLACK_THRESHOLD) {
                        if (x < X0) {
                            X0 = x;
                        }
                        if (x > X1) {
                            X1 = x;
                        }
                        if (y < Y0) {
                            Y0 = y;
                        }
                        if (y > Y1) {
                            Y1 = y;
                        }
                    }
                }
            }
            X0 = Math.Max(0, X0 - 5);
            Y0 = Math.Max(0, Y0 - 5);
            X1 = Math.Min(W - 1, X1 + 5);
            Y1 = Math.Min(H - 1, Y1 + 5);
            X1++;
            Y1++;
        }

        private Point[] calnPoints() {
            List<Point> points = new List<Point>();
            if (X1 - X0 <= 2 * CROSS_SIZE || Y1 - Y0 <= 2 * CROSS_SIZE) {
                Debug.Log("Black image");
                return points.ToArray();
            }

            sum = new int[W, H];
            for (int x = X0; x < X1; x++) {
                int p = 0;
                for (int y = Y0; y <= Y0 + 2 * CROSS_SIZE; y++) {
                    p = p + mat[x, y];
                }
                sum[x, Y0 + CROSS_SIZE] = p;
                for (int y = Y0 + CROSS_SIZE + 1; y + CROSS_SIZE < Y1; y++) {
                    p += mat[x, y + CROSS_SIZE] - mat[x, y - CROSS_SIZE - 1];
                    sum[x, y] = p;
                }
            }
            for (int y = Y0; y < Y1; y++) {
                int p = 0;
                for (int x = X0; x <= X0 + 2 * CROSS_SIZE; x++) {
                    p = p + mat[x, y];
                }
                sum[X0 + CROSS_SIZE, y] += p;
                for (int x = X0 + CROSS_SIZE + 1; x + CROSS_SIZE < X1; x++) {
                    p += mat[x + CROSS_SIZE, y] - mat[x - CROSS_SIZE - 1, y];
                    sum[x, y] += p - mat[x, y];
                }
            }

            for (int x = X0 + CROSS_SIZE + 2; x + CROSS_SIZE + 2 < X1; x++) {
                for (int y = Y0 + CROSS_SIZE + 2; y + CROSS_SIZE + 2 < Y1; y++) {
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

        private bool checkLine(int dx, int dy) {
            if (dx == 0) {
                return true;
            }
            double k = Math.Abs(dy / dx);
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
                        if (checkLine(points[i].X - points[j].X, points[i].Y - points[j].Y)) {
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

        private int anc(int[] par, int t) {
            if (par[t] != t) {
                par[t] = anc(par, par[t]);
            }
            return par[t];
        }

        private int getCentre(Point[] points) {
            int centreId = -1;
            double maxPower = 0;
            for (int i = 0; i < points.Length; i++) {
                int x = points[i].X, y = points[i].Y;
                if (x - CROSS_SIZE >= W * CENTRE_X0 && x + CROSS_SIZE < W * CENTRE_X1 && y - CROSS_SIZE >= H * CENTRE_Y0 && y + CROSS_SIZE < H * CENTRE_Y1) {
                    double power = 0;
                    for (int j = 0; j < CROSS_SIZE; j++) {
                        power += sum[x - j, y - j] + sum[x - j, y + j] + sum[x + j, y - j] + sum[x + j, y + j];
                    }
                    if (power > maxPower) {
                        maxPower = power;
                        centreId = i;
                    }
                }
            }
            return centreId;
        }

        private Point[] calnCoords(Point[] points, int[,] edges) {
            int n = points.Length;
            Point[] coords = new Point[n];
            int[] par = new int[n];

            for (int i = 0; i < n; i++) {
                par[i] = i;
            }
            List<EdgeInfo> order = new List<EdgeInfo>();

            for (int i = 0; i < n; i++) {
                for (int d = 0; d < 2; d++) {
                    int j = edges[i, d];
                    if (j != -1) {
                        order.Add(new EdgeInfo(i, j, d, points[i].DistanceTo(points[j])));
                    }
                }
            }
            order.Sort();

            for (int i = 0; i < order.Count; i++) {
                int u = order[i].u;
                int v = order[i].v;
                int d = order[i].d;
                int uid = anc(par, u);
                int vid = anc(par, v);
                if (uid != vid) {
                    par[uid] = vid;
                } else {
                    edges[u, d] = edges[v, d ^ 2] = -1;
                }
            }

            int st = getCentre(points);
            if (st == -1) {
                return coords;
            }

            par[st] = -1;
            coords[st] = new Point(C / 2, R / 2);
            int[] queue = new int[n];
            int tot = 0;
            queue[tot++] = st;
            for (int i = 0; i < tot; i++) {
                int u = queue[i];
                for (int d = 0; d < 4; d++) {
                    int v = edges[u, d];
                    if (v != -1 && par[v] != -1) {
                        par[v] = -1;
                        coords[v] = coords[u] + DIR[d];
                        queue[tot++] = v;
                    }
                }
            }

            for (int i = 0; i < n; i++) {
                if (par[i] != -1) {
                    coords[i] = new Point(-1, -1);
                }
            }

            return coords;
        }

        private int[] flatten(Point[] points, Point[] coords) {
            int[] vec = new int[R * C * 2];
            for (int i = 0; i < points.Length; i++) {
                int r = coords[i].Y;
                int c = coords[i].X;
                if (0 <= r && r < R && 0 <= c && c < C) {
                    vec[(r * C + c) * 2] = points[i].X;
                    vec[(r * C + c) * 2 + 1] = points[i].Y;
                }
            }
            return vec;
        }

        public int[] recognize(int[,] mat) {
            this.mat = mat;
            initialize();

            Point[] points = calnPoints();
            int[,] edges = calnEdges(points);
            Point[] coords = calnCoords(points, edges);
            int[] vec = flatten(points, coords);

            return vec;
        }
    }
}
